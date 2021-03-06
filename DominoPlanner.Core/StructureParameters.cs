﻿using Emgu.CV;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace DominoPlanner.Core
{
    /// <summary>
    /// Stellt die Eigenschaften und Methoden bereit, eine Struktur zu erstellen.
    /// </summary>
    [ProtoContract]
    public partial class StructureParameters : GeneralShapesProvider, ICountTargetable
    {
        // spiegelt das XElement für die Serialisierung, damit wir nicht das gesamte StructureDefinition-Objekt serialisieren müssen
        private string __structureDefXML;
        [ProtoMember(3)]
        public string _structureDefinitionXML
        {
            set
            {
                __structureDefXML = value;
                structureDefinitionXML = XElement.Parse(value);
            }
            get
            {
                return __structureDefXML;
            }
        }
        #region public properties
        /// <summary>
        /// Das XElement, das die Strukturdefinition beinhaltet.
        /// </summary>
        public XElement structureDefinitionXML
        {
            set
            {
                HasProtocolDefinition = value.Attribute("HasProtocolDefinition").Value == "true";
                name = value.Attribute("Name").Value;
                cells = new CellDefinition[3, 3];
                foreach (XElement part in value.Elements("PartDefinition"))
                {
                    int col = GetIndex(part.Attribute("HorizontalPosition").Value);
                    int row = GetIndex(part.Attribute("VerticalPosition").Value);
                    cells[col, row] = new CellDefinition(part);
                }
                __structureDefXML = value.ToString();
                shapesValid = false;
                // calculate and set characteristic lengths for Dithering
                charLength = (int)(Math.Sqrt((cells[1, 1].width * cells[1, 1].height)/cells[1,1].Count) * 1.5d);
            }
        }
        private int _length;
        /// <summary>
        /// Die Länge der Struktur (Wiederholungen des mittleren Blocks in x-Richtung)
        /// </summary>
        [ProtoMember(1)]
        public int Length
        {
            get
            {
                return _length;
            }
            set
            {
                bool hasinLeftRight = (cells == null || cells[0, 0].Count + cells[0, 1].Count + cells[0, 2].Count +
                    cells[2, 0].Count + cells[2, 1].Count + cells[2, 2].Count > 0);
                if (hasinLeftRight ? value >= 0 : value > 0 && value != _length)
                {
                    _length = value;
                    _current_width = value;
                    shapesValid = false;
                }
            }
        }
        private int _height;
        /// <summary>
        /// Die Breite der Struktur (Wiederholungen des mittleren Blocks in y-Richtung)
        /// </summary>
        [ProtoMember(2)]
        public int Height
        {
            get
            {
                return _height;
            }
            set
            {
                // Höhe 0 nur erlaubt, wenn in oberster und unterster Reihe Steine sind
                bool hasinTopBottom = (cells == null || cells[0, 0].Count + cells[1, 0].Count + cells[2, 0].Count +
                    cells[0, 2].Count + cells[1, 2].Count + cells[2, 2].Count > 0);
                if (hasinTopBottom ? value >= 0 : value > 0 && value != _height)
                {
                    _height = value;
                    shapesValid = false;
                }
            }
        }
        /// <summary>
        /// Die Zielgröße des Objekts. Setzen überschreibt Länge und Breite.
        /// </summary>

        public int TargetCount
        {
            
            // Maple sagt, dass diese Formel passt... ;)
            set
            {
                int height = PrimaryImageTreatment.Height;
                int width = PrimaryImageTreatment.Width;
                double cw = cells[1, 1].width;
                double ch = cells[1, 1].height;
                double addw = cells[0, 0].width + cells[2, 2].width;
                double addh = cells[0, 0].height + cells[2, 2].height;
                double lc = cells[0, 1].dominoes.Length;
                double tc = cells[1, 0].dominoes.Length;
                double rc = cells[2, 1].dominoes.Length;
                double bc = cells[1, 2].dominoes.Length;
                double cc = cells[1, 1].dominoes.Length;
                double constant = cells[0, 0].dominoes.Length + cells[0, 2].dominoes.Length + cells[2, 0].dominoes.Length 
                    + cells[2, 2].dominoes.Length;
                double root = Math.Sqrt(Math.Pow(cc * addw - cw * (lc + rc), 2) * Math.Pow(height, 2) +
                    (2 * (-addh * addw * cc * cc + (((-2 * constant + 2 * value) * cw + addw * (bc + tc)) * ch + cw * addh * (lc + rc)) * cc + ch * cw * (lc + rc) * (bc + tc))) * width * height
                    + Math.Pow(width, 2) * Math.Pow(-addh * cc + ch * (bc + tc), 2));
                double templength = (.5d * (root + (-cc * addw + (-lc - rc) * cw) * height + (addh * cc + (-bc - tc) * ch) * width)) / (cc * cw * height);
                double tempheight = (.5d * (root + (-addh * cc + (-bc - tc) * ch) * width + (cc * addw + (-lc - rc) * cw) * height)) / (cc * ch * width);

                if (templength < tempheight)
                {
                    Length = (int)Math.Round(templength);
                    Height = (int)Math.Round(-(-cw * Length * height + addh * width - addw * height) / (ch * width));
                }
                else
                {
                    Height = (int)Math.Round(tempheight);
                    Length = (int)Math.Round((ch * Height * width + addh * width - addw * height) / (cw * height));
                }


            }
        }
        #endregion
        #region constructors
        /// <summary>
        /// Generiert eine Struktur mit den angegebenen Wiederholparametern in x- und y-Richtung.
        /// </summary>
        /// <param name="bitmap">Das Bitmap, welchem der Struk zugrunde liegen soll.</param>
        /// <param name="definition">Die XML-Strukturdefinition, die verwendet werden soll.</param>
        /// <param name="length">Die Anzahl der Wiederholung der mittleren Zelle in x-Richtung.</param>
        /// <param name="height">Die Anzahl der Wiederholung der mittleren Zelle in y-Richtung.</param>
        /// <param name="colors">Die Farben, die für dieses Objekt verwendet werden sollen.</param>
        /// <param name="colorMode">Der Interpolationsmodus, der zur Farberkennung verwendet wird.</param>
        /// <param name="averageMode">Gibt an, ob nur ein Punkt des Dominos (linke obere Ecke) oder ein Durchschnittswert aller Pixel unter dem Pfad verwendet werden soll, um die Farbe auszuwählen.</param>
        /// <param name="allowStretch">Gibt an, ob beim Berechnen die Struktur an das Bild angepasst werden darf.</param>
        /// <param name="useOnlyMyColors">Gibt an, ob die Farben nur in der angegebenen Menge verwendet werden sollen. 
        /// Ist diese Eigenschaft aktiviert, kann das optische Ergebnis schlechter sein, das Objekt ist aber mit den angegeben Steinen erbaubar.</param>
        public StructureParameters(string filepath, string imagepath, XElement definition, int length, int height, string colors,
            IColorComparison colorMode, Dithering ditherMode, AverageMode averageMode, IterationInformation iterationInformation, bool allowStretch = false) :
            base(filepath, imagepath, colors, colorMode, ditherMode, averageMode, allowStretch, iterationInformation)
        {
            structureDefinitionXML = definition;
            this.Length = length;
            this.Height = height;
        }
        /// <summary>
        /// Generiert eine Struktur mit der angegebenen Steineanzahl.
        /// Dabei wird versucht, das Seitenverhältnis des Bildes möglichst anzunähern.
        /// </summary>
        /// <param name="bitmap">Das Bitmap, welchem der Struktur zugrunde liegen soll.</param>
        /// <param name="definition">Die XML-Strukturdefinition, die verwendet werden soll.</param>
        /// <param name="colors">Die Farben, die für dieses Objekt verwendet werden sollen.</param>
        /// <param name="colorMode">Der Interpolationsmodus, der zur Farberkennung verwendet wird.</param>
        /// <param name="averageMode">Gibt an, ob nur ein Punkt des Dominos (linke obere Ecke) oder ein Durchschnittswert aller Pixel unter dem Pfad verwendet werden soll, um die Farbe auszuwählen.</param>
        /// <param name="allowStretch">Gibt an, ob beim Berechnen die Struktur an das Bild angepasst werden darf.</param>
        /// <param name="useOnlyMyColors">Gibt an, ob die Farben nur in der angegebenen Menge verwendet werden sollen. 
        /// Ist diese Eigenschaft aktiviert, kann das optische Ergebnis schlechter sein, das Objekt ist aber mit den angegeben Steinen erbaubar.</param>
        /// <param name="targetSize">Die Zielgröße des Objekts.</param>
        public StructureParameters(string filepath, string imagepath, XElement definition, int targetSize, String colors,
            IColorComparison colorMode, Dithering ditherMode, AverageMode averageMode, IterationInformation iterationInformation, bool allowStretch = false)
            : this(filepath, imagepath, definition, 1, 1, colors, colorMode, ditherMode, averageMode, iterationInformation, allowStretch)
        {
            TargetCount = targetSize;
        }
        public StructureParameters(int imageWidth, int imageHeight, Color background, XElement definition,
            int targetSize, String colors,
            IColorComparison colorMode, Dithering ditherMode, AverageMode averageMode, IterationInformation iterationInformation, bool allowStretch = false)
            : base(imageWidth, imageHeight, background, colors, colorMode, ditherMode, averageMode, allowStretch, iterationInformation)
        {
            structureDefinitionXML = definition;
            TargetCount = targetSize;
        }
        private StructureParameters() : base() { }
        #endregion
        #region private helper methods
        public override void RegenerateShapes()
        {
            last = GenerateStructure(Length, Height);
            shapesValid = true;
        }

        
        #endregion
    }
}
