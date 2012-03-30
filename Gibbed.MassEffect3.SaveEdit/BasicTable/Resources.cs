﻿/* Copyright (c) 2012 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System.Collections.Generic;
using System.Windows.Forms;

namespace Gibbed.MassEffect3.SaveEdit.BasicTable
{
    internal static class Resources
    {
        public static List<TableItem> Build(Editor editor)
        {
            return new List<TableItem>()
            {
                new TableItem()
                {
                    Name = SaveEdit.Resources.Localization.Editor_BasicTable_Resources_Credits,
                    Control = new NumericUpDown()
                    {
                        Minimum = int.MinValue,
                        Maximum = int.MaxValue,
                        Increment = 1,
                        Dock = DockStyle.Fill,
                    },
                    Binding =
                        new Binding("Value",
                                    editor.saveFileBindingSource,
                                    "Player.Credits",
                                    true,
                                    DataSourceUpdateMode.OnPropertyChanged)
                },
                new TableItem()
                {
                    Name = SaveEdit.Resources.Localization.Editor_BasicTable_Resources_Medigel,
                    Control = new NumericUpDown()
                    {
                        Minimum = int.MinValue,
                        Maximum = int.MaxValue,
                        Increment = 1,
                        Dock = DockStyle.Fill,
                    },
                    Binding =
                        new Binding("Value",
                                    editor.saveFileBindingSource,
                                    "Player.Medigel",
                                    true,
                                    DataSourceUpdateMode.OnPropertyChanged)
                },
                new TableItem()
                {
                    Name = SaveEdit.Resources.Localization.Editor_BasicTable_Resources_ExtraMedigel,
                    Control = new NumericUpDown()
                    {
                        Minimum = int.MinValue,
                        Maximum = int.MaxValue,
                        Increment = 1,
                        Dock = DockStyle.Fill,
                    },
                    Binding =
                        new Binding("Value",
                                    editor.saveFileBindingSource,
                                    "Plot.Helpers.ExtraMedigel",
                                    true,
                                    DataSourceUpdateMode.OnPropertyChanged)
                },
                new TableItem()
                {
                    Name = SaveEdit.Resources.Localization.Editor_BasicTable_Resources_Grenades,
                    Control = new NumericUpDown()
                    {
                        Minimum = int.MinValue,
                        Maximum = int.MaxValue,
                        Increment = 1,
                        Dock = DockStyle.Fill,
                    },
                    Binding =
                        new Binding("Value",
                                    editor.saveFileBindingSource,
                                    "Player.Grenades",
                                    true,
                                    DataSourceUpdateMode.OnPropertyChanged)
                },
            };
        }
    }
}
