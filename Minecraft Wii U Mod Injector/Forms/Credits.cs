﻿using System;
using System.Diagnostics;
using System.Windows.Forms;
using MetroFramework.Forms;

namespace Minecraft_Wii_U_Mod_Injector.Forms {
    public partial class Credits : MetroForm {
        public Credits(MainForm injector) {
            InitializeComponent();
            StyleMngr.Style = this.Style = injector.StyleMngr.Style;
            StyleMngr.Theme = this.Theme = injector.StyleMngr.Theme;
        }

        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                cp.ExStyle = cp.ExStyle | 0x02000000;
                return cp;
            }
        }

        private void KashiieraClicked(object sender, EventArgs e) {
            Process.Start("https://www.youtube.com/Kashiiera");
        }

        private void BullyClicked(object sender, EventArgs e) {
            Process.Start("https://www.youtube.com/BullyWiiPlaza");
        }

        private void VylrynaClicked(object sender, EventArgs e) {
            Process.Start("https://www.youtube.com/vylryna");
        }

        private void Exiting(object sender, FormClosingEventArgs e) {
            Dispose();
        }
    }
}
