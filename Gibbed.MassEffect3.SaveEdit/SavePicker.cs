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

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Gibbed.MassEffect3.SaveEdit
{
    public partial class SavePicker : Form
    {
        public string FilePath;
        public string SelectedPath;

        public FileFormats.SaveFile SaveFile = null;
        private int HighestSaveNumber = 0;

        private PickerMode _FileMode = PickerMode.Invalid;
        public PickerMode FileMode
        {
            get { return this._FileMode; }
            set
            {
                if (value != this._FileMode)
                {
                    this._FileMode = value;
                    this.loadFileButton.Visible = value == PickerMode.Load;
                    this.saveFileButton.Visible = value == PickerMode.Save;
                }
            }
        }

        public enum PickerMode
        {
            Invalid,
            Load,
            Save,
        }

        public SavePicker()
        {
            this.InitializeComponent();
            this.FileMode = PickerMode.Load;

            /* This following block is for Mono-build compatability
             * (ie, compiling this code via Mono and running via .NET)
             * 
             * Mono developers are asstwats:
             *   https://bugzilla.novell.com/show_bug.cgi?id=641826
             * 
             * So, instead of using the ImageListStreamer directly, we'll
             * load images from resources.
             */
            this.iconImageList.Images.Clear();
            this.iconImageList.Images.Add("Unknown", new System.Drawing.Bitmap(16, 16));
            this.iconImageList.Images.Add("New", Icons.New);
            this.iconImageList.Images.Add("Class_Adept", Icons.Cat);
            this.iconImageList.Images.Add("Class_Soldier", Icons.Monkey);
            this.iconImageList.Images.Add("Class_Engineer", Icons.Robot);
            this.iconImageList.Images.Add("Class_Sentinel", Icons.Dog);
            this.iconImageList.Images.Add("Class_Infiltrator", Icons.Ghost);
            this.iconImageList.Images.Add("Class_Vanguard", Icons.Penguin);

            this.careerListView.Items.Clear();
            this.careerListView.Items.Add(new ListViewItem()
                {
                    Text = "(new career)",
                    ImageKey = "New",
                });

            this.saveListView.Items.Clear();
            this.saveListView.Items.Add(new ListViewItem()
            {
                Text = "(new save)",
                ImageKey = "New",
            });
        }

        private enum PlayerClass
        {
            Invalid,
            Adept,
            Soldier,
            Engineer,
            Sentinel,
            Infiltrator,
            Vanguard,
        }

        private static bool ParseCareerName(
            string input,
            out string name,
            out FileFormats.Save.OriginType originType,
            out FileFormats.Save.NotorietyType reputationType,
            out PlayerClass classType,
            out DateTime date)
        {
            name = null;
            classType = PlayerClass.Invalid;
            originType = FileFormats.Save.OriginType.None;
            reputationType = FileFormats.Save.NotorietyType.None;
            date = DateTime.Now;

            var parts = input.Split('_');
            if (parts.Length != 5)
            {
                return false;
            }

            name = parts[0];

            if (parts[1] == null ||
                parts[1].Length != 2)
            {
                return false;
            }

            switch (parts[1][0])
            {
                case '0': originType = FileFormats.Save.OriginType.None; break;
                case '1': originType = FileFormats.Save.OriginType.Spacer; break;
                case '2': originType = FileFormats.Save.OriginType.Colony; break;
                case '3': originType = FileFormats.Save.OriginType.Earthborn; break;
                default: return false;
            }

            switch (parts[1][1])
            {
                case '0': reputationType = FileFormats.Save.NotorietyType.None; break;
                case '1': reputationType = FileFormats.Save.NotorietyType.Survivor; break;
                case '2': reputationType = FileFormats.Save.NotorietyType.Warhero; break;
                case '3': reputationType = FileFormats.Save.NotorietyType.Ruthless; break;
                default: return false;
            }

            if (parts[2] == null ||
                Enum.TryParse(parts[2], true, out classType) == false)
            {
                return false;
            }

            if (parts[3] == null ||
                parts[3].Length != 6)
            {
                return false;
            }

            if (parts[4] == null ||
                parts[4].Length != 7)
            {
                return false;
            }

            int day;
            if (int.TryParse(parts[3].Substring(0, 2), out day) == false)
            {
                return false;
            }

            int month;
            if (int.TryParse(parts[3].Substring(2, 2), out month) == false)
            {
                return false;
            }

            int year;
            if (int.TryParse(parts[3].Substring(4, 2), out year) == false)
            {
                return false;
            }

            date = new DateTime(2000 + year, month, day);
            return true;
        }

        private void FindCareers()
        {
            this.careerListView.BeginUpdate();

            this.careerListView.Items.Clear();

            if (Directory.Exists(this.FilePath) == true)
            {
                foreach (var careerPath in Directory
                    .GetDirectories(this.FilePath)
                    .OrderByDescending(d => Directory.GetLastWriteTime(d)))
                {
                    var careerFiles = Directory.GetFiles(careerPath, "*.pcsav");
                    if (careerFiles.Length == 0)
                    {
                        continue;
                    }

                    FileFormats.SaveFile saveFile = null;
                    foreach (var careerFile in careerFiles)
                    {
                        try
                        {
                            using (var input = File.OpenRead(careerFiles[0]))
                            {
                                saveFile = FileFormats.SaveFile.Read(input);
                            }
                            
                            break;
                        }
                        catch (Exception)
                        {
                            saveFile = null;
                        }
                    }

                    // attempt to parse the directory name
                    string name;
                    FileFormats.Save.OriginType originType;
                    FileFormats.Save.NotorietyType reputationType;
                    PlayerClass classType;
                    DateTime date;

                    if (ParseCareerName(
                        Path.GetFileName(careerPath),
                        out name, out originType, out reputationType,
                        out classType,
                        out date) == true)
                    {
                        string displayName = "";
                        displayName += (saveFile == null ? name : saveFile.Player.FirstName) + "\n";
                        displayName += string.Format("{0}, {1}",
                            classType,
                            date.ToString("d"));
                        //displayName += date.ToString();

                        this.careerListView.Items.Add(new ListViewItem()
                            {
                                Text = displayName,
                                ImageKey = "Class_" + classType.ToString(),
                                Tag = careerPath,
                            });
                    }
                    else
                    {
                        this.careerListView.Items.Add(new ListViewItem()
                            {
                                Text = Path.GetFileName(careerPath),
                                ImageKey = "",
                                Tag = careerPath,
                            });
                    }
                }
            }

            if (this.FileMode == PickerMode.Save)
            {
                if (this.careerListView.Items.Count > 0)
                {
                    this.careerListView.Items.Insert(1, new ListViewItem()
                        {
                            Name = "New Career",
                            Text = "(new career)",
                            ImageKey = "New",
                        });
                }
                else
                {
                    this.careerListView.Items.Add(new ListViewItem()
                        {
                            Name = "New Career",
                            Text = "(new career)",
                            ImageKey = "New",
                        });
                }
            }

            this.careerListView.EndUpdate();

            if (this.careerListView.Items.Count > 0)
            {
                this.careerListView.Items[0].Selected = true;
            }
            else
            {
                this.FindSaves(null);
            }
        }

        private void FindSaves(string savePath)
        {
            this.saveListView.BeginUpdate();

            this.saveListView.Items.Clear();
            if (this.FileMode == PickerMode.Save)
            {
                this.saveListView.Items.Add(new ListViewItem()
                {
                    Name = "New Save",
                    Text = "(new save)",
                    ImageKey = "New",
                });
            }
            
            this.HighestSaveNumber = 0;
            if (savePath != null)
            {
                if (Directory.Exists(this.FilePath) == true)
                {
                    foreach (var inputPath in Directory
                        .GetFiles(savePath, "*.pcsav")
                        .OrderByDescending(f => Directory.GetLastWriteTime(f)))
                    {
                        var baseName = Path.GetFileNameWithoutExtension(inputPath);
                        if (baseName.StartsWith("Save_") == true &&
                            baseName.Length == 9)
                        {
                            int saveNumber;
                            if (int.TryParse(baseName.Substring(5).TrimStart('0'), out saveNumber) == true)
                            {
                                this.HighestSaveNumber = Math.Max(saveNumber, this.HighestSaveNumber);
                            }
                        }

                        FileFormats.SaveFile saveFile = null;
                        try
                        {
                            using (var input = File.OpenRead(inputPath))
                            {
                                saveFile = FileFormats.SaveFile.Read(input);
                            }
                        }
                        catch (Exception)
                        {
                            saveFile = null;
                        }

                        this.saveListView.Items.Add(new ListViewItem()
                        {
                            Text = Path.GetFileName(inputPath),
                            ImageKey = "",
                            Tag = inputPath,
                        });
                    }
                }
            }

            this.saveListView.EndUpdate();

            if (this.saveListView.Items.Count > 0)
            {
                this.saveListView.Items[0].Selected = true;
            }
        }

        private void OnShown(object sender, EventArgs e)
        {
            this.loadFileButton.Enabled = false;
            this.saveFileButton.Enabled = false;
            this.FindCareers();
        }

        private void OnSelectCareer(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            this.loadFileButton.Enabled = false;
            this.saveFileButton.Enabled = false;
            this.deleteSaveButton.Enabled = false;

            if (e.IsSelected == true)
            {
                this.FindSaves(e.Item.Tag as string);
                this.deleteCareerButton.Enabled = e.Item.Tag is string;
            }
            else
            {
                this.deleteCareerButton.Enabled = false;
                this.FindSaves(null);
            }
        }

        private void OnSelectSave(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            this.loadFileButton.Enabled = false;
            this.saveFileButton.Enabled = false;
            this.deleteSaveButton.Enabled = false;

            if (e.IsSelected == true)
            {
                if (e.Item.Name == "New Save" ||
                    e.Item.Tag is string)
                {
                    this.loadFileButton.Enabled = true;
                    this.saveFileButton.Enabled = true;
                    this.deleteSaveButton.Enabled = e.Item.Tag is string;
                }
            }
        }

        private static string FilterPath(string path)
        {
            var sb = new StringBuilder();
            foreach (var c in path)
            {
                if ((c >= 'A' && c <= 'Z') ||
                    (c >= 'a' && c <= 'z'))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static string TranslateClass(int index)
        {
            switch (index)
            {
                case 93954: return "Adept";
                case 93952: return "Soldier";
                case 93953: return "Engineer";
                case 93957: return "Sentinel";
                case 93955: return "Infiltrator";
                case 93956: return "Vanguard";
                default: return "Unknown";
            }
        }

        private string GetSelectedPath(out bool exists)
        {
            if (this.saveListView.SelectedItems.Count > 0 &&
                (this.saveListView.SelectedItems[0].Tag is string) == true)
            {
                exists = true;
                return (string)this.saveListView.SelectedItems[0].Tag;
            }

            exists = false;

            if (this.FileMode == PickerMode.Load)
            {
                return null;
            }

            if (this.careerListView.SelectedItems.Count == 0)
            {
                return null;
            }

            var path = this.FilePath;
            
            int saveNumber;
            if (this.careerListView.SelectedItems[0].Name == "New Career")
            {
                var name = string.Format("{0}_{1}{2}_{3}_{4}_{5}",
                    FilterPath(this.SaveFile.Player.FirstName),
                    (int)this.SaveFile.Player.Origin,
                    (int)this.SaveFile.Player.Notoriety,
                    TranslateClass(this.SaveFile.Player.ClassFriendlyName),
                    DateTime.Now.ToString("ddMMyy", CultureInfo.InvariantCulture),
                    BitConverter.ToString(this.SaveFile.Player.CharacterGUID.ToByteArray()).Replace("-", "").Substring(0, 7));
                path = Path.Combine(path, name);
                saveNumber = 0;
            }
            else if ((this.careerListView.SelectedItems[0].Tag is string) == false)
            {
                return null;
            }
            else
            {
                path = (string)this.careerListView.SelectedItems[0].Tag;
                saveNumber = this.HighestSaveNumber + 1;
            }

            path = Path.Combine(path, string.Format("Save_{0}.pcsav",
                saveNumber.ToString().PadLeft(4, '0')));
            return path;
        }

        private void OnChooseSave(object sender, EventArgs e)
        {
            bool exists;
            var path = this.GetSelectedPath(out exists);
            if (path == null)
            {
                return;
            }

            if (this.FileMode == PickerMode.Save &&
                exists == true)
            {
                if (MessageBox.Show(
                        "Are you sure you want to overwrite this save file?",
                        "Question",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.No)
                {
                    return;
                }
            }

            this.SelectedPath = path;
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }

        private void OnCancel(object sender, EventArgs e)
        {
            this.SelectedPath = null;
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

        private void OnDeleteCareer(object sender, EventArgs e)
        {
            if (this.careerListView.SelectedItems.Count == 0 ||
                (this.careerListView.SelectedItems[0].Tag is string) == false)
            {
                return;
            }

            if (MessageBox.Show(
                "Are you sure you want to delete this career?",
                "Warning",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.No)
            {
                return;
            }

            this.careerListView.BeginUpdate();
            var item = this.careerListView.SelectedItems[0];
            this.careerListView.Items.Remove(item);

            var basePath = (string)item.Tag;
            var savePaths = Directory.GetFiles(basePath, "*.pcsav");
            if (savePaths.Length > 0)
            {
                foreach (var savePath in savePaths)
                {
                    File.Delete(savePath);
                }
                try
                {
                    Directory.Delete(basePath);
                }
                catch (IOException ex)
                {
                    MessageBox.Show(
                        "There was an error removing the directory:\n" +
                        ex.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }

            this.careerListView.EndUpdate();
            this.FindCareers();
        }

        private void OnDeleteSave(object sender, EventArgs e)
        {
            if (this.saveListView.SelectedItems.Count == 0 ||
                (this.saveListView.SelectedItems[0].Tag is string) == false)
            {
                return;
            }

            if (MessageBox.Show(
                "Are you sure you want to delete this save?",
                "Warning",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.No)
            {
                return;
            }

            this.saveListView.BeginUpdate();
            var item = this.saveListView.SelectedItems[0];
            this.saveListView.Items.Remove(item);

            var savePath = (string)item.Tag;

            try
            {
                File.Delete(savePath);
            }
            catch (IOException ex)
            {
                MessageBox.Show(
                    "There was an error removing the file:\n" +
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            this.saveListView.EndUpdate();
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            this.FindCareers();
        }
    }
}