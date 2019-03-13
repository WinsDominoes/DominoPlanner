﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominoPlanner.Core
{
    partial class StructureParameters : ICopyPasteable, IRowColumnAddableDeletable
    {
        public void ResetSize()
        {
            current_width = Length;
        }
        private int _current_width;
        public int current_width { get => _current_width; set => _current_width = value; }

        public int current_height
        {
            get
            {
                return (last.length - getLengthOfRow(0, current_width) - getLengthOfRow(2, current_width)) 
                    / getLengthOfRow(1,_current_width);
            }
            set { }
        }

        public bool IsValidPastePosition(int source_position, int target_position)
        {
            var source =getPositionFromIndex(source_position);
            var target = getPositionFromIndex(target_position);
            if (source.CountInsideCell == target.CountInsideCell)
            {
                if (source.X > -1 && source.Y > -1 && source.X < current_width && source.Y < current_height)
                {
                    if (target.X > -1 && target.Y > -1 && target.X < current_width && target.Y < current_height)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public int[] GetValidPastePositions(int source_position)
        {
            var positions = new int[current_width * current_height];
            var source = getPositionFromIndex(source_position);
            if (!(source.X != -1 && source.Y != -1 && source.X != current_width && source.Y != current_height))
            {
                throw new InvalidOperationException("Can't copy from here");
            }
            for (int x = 0; x < current_width; x++)
            {
                for (int y = 0; y < current_height; y++)
                {
                    positions[x * current_height + y] = getIndexFromPosition(y, x, source.CountInsideCell);
                }
            }
            return positions;
        }

        public int[] PasteTarget(int reference, int[] source_domain, int target_reference)
        {
            int[] target_domain = new int[source_domain.Length];
            if (!IsValidPastePosition(reference, target_reference)) throw new InvalidOperationException("Can't paste here");
            var source = getPositionFromIndex(reference);
            var target = getPositionFromIndex(target_reference);
            var rowshift = target.Y - source.Y;
            var colshift = target.X - source.X;
            for (int i = 0; i < source_domain.Length; i++)
            {
                var current = getPositionFromIndex(source_domain[i]);
                var target_index = getIndexFromPosition(current.Y + rowshift, current.X + colshift, current.CountInsideCell);
                // nur im "mittleren" Bereich darf gepastet werden
                target_domain[i] = current.Y + rowshift < current_height && current.X + colshift < current_width ? target_index : last.length;
            }
            return target_domain;
        }
        public PositionWrapper getPositionFromIndex(int index)
        {
            int[] row_counts = Enumerable.Range(0, 2).Select(x => getLengthOfRowColumn(false, x)).ToArray();
            int reihe = (index < row_counts[0]) ? -1 : 
                ((index < row_counts[0] + row_counts[1] * current_height) ? (index - row_counts[0]) / row_counts[1] : current_height);
            int steine_vor_reihe = (reihe == -1 ? 0 : row_counts[0] + reihe * row_counts[1]);
            int reihentyp = getTyp(reihe, false, current_width, current_height);
            int index_in_reihe = index - steine_vor_reihe;
            int spalte = (index_in_reihe < cells[0, reihentyp].Count) ? -1 :
                ((index_in_reihe < cells[0, reihentyp].Count + cells[1, reihentyp].Count * current_width) 
                    ? (index_in_reihe - cells[0, reihentyp].Count) / cells[1, reihentyp].Count : current_width);
            int steine_vor_zelle = (spalte == -1 ? 0 : cells[0, reihentyp].Count + spalte * cells[1, reihentyp].Count);
            int index_in_zelle = index_in_reihe - steine_vor_zelle;
            return new PositionWrapper() { X = spalte, Y = reihe, CountInsideCell = index_in_zelle };
        }


        public PositionWrapper getPositionFromIndex(int index, int new_width, int new_height)
        {
            return getPositionFromIndex(index);
        }
        public int getIndexFromPosition(int reihe, int spalte, int index_in_zelle, int width, int height, bool swap = false)
        {
            if (swap)
            {
                int temp = reihe; reihe = spalte; spalte = temp;
            }
            int summe_reihen_davor = reihe == -1 ? 0 : getLengthOfRowColumn(false, 1, width, height) * reihe
                + getLengthOfRowColumn(false, 0, width, height);
            int reihentyp = getTyp(reihe, false, width, height);
            int summe_spalten_davor = (spalte == -1 ? 0 : cells[0, reihentyp].Count + spalte * cells[1, reihentyp].Count);
            return summe_reihen_davor + summe_spalten_davor + index_in_zelle;
        }
        public int getTyp(int index, bool column, int width, int height)
        {
            if (index == -1) return 0;
            if ((column && index == width) || (!column && index == height)) return 2;
            return 1;
        }
        public int getIndexFromPosition(int reihe, int spalte, int index_in_zelle, bool swap = false)
        {
            return getIndexFromPosition(reihe, spalte, index_in_zelle, current_width, current_height, swap);
        }
        public int getIndexFromPosition(PositionWrapper wrapper)
        {
            return getIndexFromPosition(wrapper.Y, wrapper.X, wrapper.CountInsideCell);
        }

        public void copyGroup(IDominoShape[] target, int source_x, int source_y, int target_x, int target_y, int target_width, int target_height)
        {
            if (source_x >= -1 && source_x <= current_width && source_y >= -1 && source_y <= current_height)
            {
                if (target_x >= -1 && target_x <= target_width && target_y >= -1 && target_y <= target_height)
                {
                    var (targetindex, target_rowtyp, target_coltyp) = getIndexUndTyp(target_y, target_x, 0, target_width, target_height, false);
                    var (sourceindex, source_rowtyp, source_coltyp) = getIndexUndTyp(source_y, source_x, 0, current_width, current_height, false);
                    if (target_rowtyp != source_rowtyp || target_coltyp != source_coltyp) throw new InvalidOperationException("type mismatch");
                    for (int i=0; i < cells[target_coltyp, target_rowtyp].Count; i++)
                    {
                        if (targetindex + i >= target.Length) break;
                        target[targetindex + i].color = last.shapes[sourceindex + i].color;
                    }
                }
            }
        }
        public IDominoShape[] getNewShapes(int current_width, int current_height)
        {
            List<IDominoShape> DominoList = new List<IDominoShape>();
           
            for (int y = -1; y < current_height + 1; y++)
            {
                for (int x = -1; x < current_width + 1; x++)
                {
                    DominoList.AddRange(
                        (cells[getTyp(x, true, current_width, current_height), getTyp(y, false, current_width, current_height)]
                        .TransformDefinition(
                            (x == -1) ? 0 : (cells[1, 1].width * x + cells[0, 0].width),
                            (y == -1) ? 0 : (cells[1, 1].height * y + cells[0, 0].height),
                            x, y, current_width, current_height))
                        .dominoes);
                }
            }
            return DominoList.ToArray();
        }

        public int[] ExtractRowColumn(bool column, int index)
        {
            int[] result = new int[getLengthOfTypicalRowColumn(column)];
            int position = 0;
            for (int i = -1; i <= (column ? current_height : current_width); i++)
            {
                var (startindex, rowtyp, coltyp) = getIndexUndTyp(index, i, 0, current_width, current_height, column);
                for (int j = 0; j < cells[coltyp, rowtyp].Count; j++)
                {
                    result[position] = last.shapes[startindex + j].color;
                    position++;
                }
            }
            return result;
        }

        public void ReinsertRowColumn(int[] rowcolumn, bool column, int index, IDominoShape[] target, int target_width, int target_height)
        {
            int position = 0;
            for (int i = -1; i <= (column ? target_height : target_width); i++)
            {
                var (startindex, rowtyp, coltyp) = getIndexUndTyp( index, i, 0, target_width, target_height, column);
                for (int j = 0; j < cells[coltyp, rowtyp].Count; j++)
                {
                    target[startindex + j].color = rowcolumn[position];
                    position++;
                }
            }
        }
        public int getLengthOfTypicalRowColumn(bool column)
        {
            return getLengthOfRowColumn(column, 1);
        }
        private int getLengthOfRowColumn(bool column, int typ, int width, int height)
        {
            if (column)
                return cells[typ, 0].Count + height * cells[typ, 1].Count + cells[typ, 2].Count;
            else return getLengthOfRow(typ, width);
        }
        private int getLengthOfRow(int typ, int width)
        {
            return cells[0, typ].Count + width * cells[1, typ].Count + cells[2, typ].Count;
        }
        private int getLengthOfRowColumn(bool column, int typ)
        {
            return getLengthOfRowColumn(column, typ, current_width, current_height);
        }
        private (int startindex, int rowtyp, int coltyp) getIndexUndTyp(int row, int column, int index, int width, int height, bool swap)
        {
            int startindex = getIndexFromPosition(row, column, index, width, height, swap);
            int rowtyp = getTyp(swap ? column : row, false, width, height);
            int coltyp = getTyp(swap ? row : column, true, width, height);
            return (startindex, rowtyp, coltyp);
        }

        public (int startindex, int endindex) getIndicesOfCell(int row, int col, int target_width, int target_height)
        {
            if (row >= -1 && col >= -1 && row <= target_height && col <= target_width)
            {
                var (targetindex, target_rowtyp, target_coltyp) = getIndexUndTyp(row, col, 0, target_width, target_height, false);
                return (targetindex, targetindex + cells[target_coltyp, target_rowtyp].Count - 1);
            }
            return (0, -1);
        }
    }
}
