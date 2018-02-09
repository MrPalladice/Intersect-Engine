﻿using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Intersect.Editor.Classes.Core;
using Intersect.GameObjects.Events;
using Intersect.Localization;
using Intersect.Utilities;

namespace Intersect.Editor.Forms.Editors.Event_Commands
{
    public partial class EventCommandText : UserControl
    {
        private readonly FrmEvent mEventEditor;
        private EventCommand mMyCommand;

        public EventCommandText(EventCommand refCommand, FrmEvent editor)
        {
            InitializeComponent();
            mMyCommand = refCommand;
            mEventEditor = editor;
            InitLocalization();
            txtShowText.Text = mMyCommand.Strs[0];
            cmbFace.Items.Clear();
            cmbFace.Items.Add(Strings.Get("general", "none"));
            cmbFace.Items.AddRange(GameContentManager.GetSmartSortedTextureNames(GameContentManager.TextureType.Face));
            if (cmbFace.Items.IndexOf(TextUtils.NullToNone(mMyCommand.Strs[1])) > -1)
            {
                cmbFace.SelectedIndex = cmbFace.Items.IndexOf(TextUtils.NullToNone(mMyCommand.Strs[1]));
            }
            else
            {
                cmbFace.SelectedIndex = 0;
            }
            UpdateFacePreview();
        }

        private void InitLocalization()
        {
            grpShowText.Text = Strings.Get("eventshowtext", "title");
            lblText.Text = Strings.Get("eventshowtext", "text");
            lblFace.Text = Strings.Get("eventshowtext", "face");
            lblCommands.Text = Strings.Get("eventshowtext", "commands");
            btnSave.Text = Strings.Get("eventshowtext", "okay");
            btnCancel.Text = Strings.Get("eventshowtext", "cancel");
        }

        private void UpdateFacePreview()
        {
            if (File.Exists("resources/faces/" + cmbFace.Text))
            {
                pnlFace.BackgroundImage = new Bitmap("resources/faces/" + cmbFace.Text);
            }
            else
            {
                pnlFace.BackgroundImage = null;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            mMyCommand.Strs[0] = txtShowText.Text;
            mMyCommand.Strs[1] = TextUtils.SanitizeNone(cmbFace?.Text);
            mEventEditor.FinishCommandEdit();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            mEventEditor.CancelCommandEdit();
        }

        private void cmbFace_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateFacePreview();
        }

        private void lblCommands_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(
                "http://www.ascensiongamedev.com/community/topic/749-event-text-variables/");
        }
    }
}