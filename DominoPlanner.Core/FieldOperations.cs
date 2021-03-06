﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominoPlanner.Core
{
    public partial class FieldParameters : ICopyPasteable, IRowColumnAddableDeletable
    {
        public void ResetSize()
        {
            current_width = Length;
        }
        [ProtoMember(50)]
        public int current_width { get; set; }
        public int current_height { get => last.length / current_width; set { } }
        public bool IsValidPastePosition(int source_position, int target_position)
        {
            return true;
        }
        public int[] GetValidPastePositions(int source_position)
        {
            return Enumerable.Range(0, last.length).ToArray();
        }
        public int[] PasteTarget(int reference, int[] source_domain, int target_reference)
        {
            int colshift = target_reference % current_width - reference % current_width;
            int rowshift = target_reference / current_width - reference / current_width;
            int[] target_domain = new int[source_domain.Length];
            for (int i = 0; i < source_domain.Length; i++)
            {
                //shapes[length * yi + xi]
                int row = source_domain[i] / current_width;
                int col = source_domain[i] % current_width;
                target_domain[i] = (row + rowshift < current_height && col + colshift < current_width) ? 
                    (row + rowshift) * (current_width) + (col + colshift) : last.length;
            }
            return target_domain;
        }

        public PositionWrapper getPositionFromIndex(int index)
        {
            return getPositionFromIndex(index, current_width, current_height);
        }

        public int getIndexFromPosition(PositionWrapper wrapper)
        {
            return wrapper.X + current_width * wrapper.Y;
        }

        public int getIndexFromPosition(int row, int col, int index, bool swap_coords = false)
        {
            return getIndexFromPosition(row, col, index, current_width, current_height, swap_coords);
        }

        public int getIndexFromPosition(int row, int col, int index, int new_width, int new_height, bool swap_coords = false)
        {
            return swap_coords ? row + new_width * col : col + new_width * row;
        }

        public void copyGroup(IDominoShape[] target, 
            int source_x, int source_y, int target_x, int target_y, int target_width, int target_height)
        {
            if (source_x >= 0 && source_x < current_width && source_y >= 0 && source_y < current_height)
            {
                if (target_x >= 0 && target_x < target_width && target_y >= 0 && target_y < target_height)
                {
                    int target_index = getIndexFromPosition(target_y, target_x, 0, target_width, target_height);
                    int source_index = getIndexFromPosition(source_y, source_x, 0);
                    target[target_index].color = this.last.shapes[source_index].color;
                }
            }
            
        }

        public IDominoShape[] getNewShapes(int length, int height)
        {
            IDominoShape[] array = new IDominoShape[length * height];

            Parallel.For(0, length, new ParallelOptions { MaxDegreeOfParallelism = -1 }, (xi) =>
            {
                for (int yi = 0; yi < height; yi++)
                {
                    RectangleDomino shape = new RectangleDomino()
                    {
                        x = (HorizontalDistance + HorizontalSize) * xi,
                        y = (VerticalDistance + VerticalSize) * yi,
                        width = HorizontalSize,
                        height = VerticalSize,
                        expanded_width = HorizontalDistance + HorizontalSize,
                        expanded_height = VerticalDistance + VerticalSize,
                        position = new ProtocolDefinition() { x = xi, y = yi }
                    };
                    array[length * yi + xi] = shape;
                }
            });
            return array;
        }

        public int[] ExtractRowColumn(bool column, int index)
        {
            int[] result = new int[getLengthOfTypicalRowColumn(column)];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = last.shapes[getIndexFromPosition(index, i, 0, column)].color;
            }
            return result;
        }

        public void ReinsertRowColumn(int[] rowcolumn, bool column, int index, IDominoShape[] target, int target_width, int target_height)
        {
            for (int i = 0; i < rowcolumn.Length; i++)
            {
                int targetindex = getIndexFromPosition(index, i, 0, target_width, target_height, column);
                target[targetindex].color
                    = rowcolumn[i];
            }
        }

        public int getLengthOfTypicalRowColumn(bool column)
        {
            return column ? current_height : current_width;
        }

        public (int startindex, int endindex) getIndicesOfCell(int row, int col, int target_width, int target_height)
        {
            if (row < target_height && col < target_width && row >= 0 && col >= 0)
            {
                int index = getIndexFromPosition(row, col, 0, target_width, target_height, false);
                return (index, index);
            }
            return (0, -1);
        }

        public PositionWrapper getPositionFromIndex(int index, int new_width, int new_height)
        {
            return new PositionWrapper() { X = index % new_width, Y = index / new_width, CountInsideCell = 0 };
        }
    }
}
