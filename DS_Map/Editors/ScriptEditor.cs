using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using LiTRE.Resources;
using LiTRE.ROMFiles;
using ScintillaNET;
using ScintillaNET.Utils;
using System.Globalization;
using static OpenTK.Graphics.OpenGL.GL;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace LiTRE.Editors
{
    public partial class ScriptEditor : UserControl
    {
        public bool scriptEditorIsReady { get; set; } = false;
        private Scintilla ScriptTextArea;
        private Scintilla FunctionTextArea;
        private Scintilla ActionTextArea;
        private SearchManager scriptSearchManager;
        private SearchManager functionSearchManager;
        private SearchManager actionSearchManager;
        private Scintilla currentScintillaEditor;
        private SearchManager currentSearchManager;
        private bool scriptsDirty = false;
        private bool functionsDirty = false;
        private bool actionsDirty = false;
        private string cmdKeyWords = "";
        private string secondaryKeyWords = "";
        private string altCaseKeywords = "";
        private ScriptFile currentScriptFile;
        MainProgram _parent;

        /// <summary>
        /// the background color of the text area
        /// </summary>
        private readonly Color BACK_COLOR = Color.FromArgb(0x2A211C);

        /// <summary>
        /// default text color of the text area
        /// </summary>
        private readonly Color FORE_COLOR = Color.FromArgb(0xB7B7B7);

        /// <summary>
        /// change this to whatever margin you want the line numbers to show in
        /// </summary>
        private const int NUMBER_MARGIN = 1;

        /// <summary>
        /// change this to whatever margin you want the bookmarks/breakpoints to show in
        /// </summary>
        private const int BOOKMARK_MARGIN = 2;

        private const int BOOKMARK_MARKER = 2;

        /// <summary>
        /// change this to whatever margin you want the code folding tree (+/-) to show in
        /// </summary>
        private const int FOLDING_MARGIN = 3;

        /// <summary>
        /// set this true to show circular buttons for code folding (the [+] and [-] buttons on the margin)
        /// </summary>
        private const bool CODE_FOLDING_CIRCULAR = true;

        readonly Point initial_importScriptFileButton_location;
        readonly Point initial_exportScriptFileButton_location;
        readonly Point initial_addScriptFileButton_location;
        readonly Point initial_removeScriptFileButton_location;
        readonly Point initial_viewLevelScript_location;

        readonly Point new_importScriptFileButton_location;
        readonly Point new_exportScriptFileButton_location;
        readonly Point new_addScriptFileButton_location;
        readonly Point new_removeScriptFileButton_location;
        readonly Point new_viewLevelScript_location;

        public ScriptEditor()
        {
            InitializeComponent();


            //initially, these buttons are off the canvas so they can be interacted with in the designer
            //they are then moved as needed
            initial_importScriptFileButton_location = importScriptFileButton.Location;
            initial_exportScriptFileButton_location = exportScriptFileButton.Location;
            initial_addScriptFileButton_location = addScriptFileButton.Location;
            initial_removeScriptFileButton_location = removeScriptFileButton.Location;
            initial_viewLevelScript_location = viewLevelScriptButton.Location;

            new_importScriptFileButton_location = new Point(164, 22);
            new_exportScriptFileButton_location = new Point(239, 22);
            new_addScriptFileButton_location = new Point(314, 22);
            new_removeScriptFileButton_location = new Point(314, 49);
            new_viewLevelScript_location = new Point(326, 37);

            importScriptFileButton.Enabled = false;
            exportScriptFileButton.Enabled = false;
            addScriptFileButton.Enabled = false;
            removeScriptFileButton.Enabled = false;
            viewLevelScriptButton.Enabled = false;
            //if(SettingsManager.Settings.vscPath == string.Empty)
            //{
            //    openInVSC.Enabled = false;
            //}
        }

        public void SetupScriptEditor(MainProgram parent, bool force = false)
        {
            if (scriptEditorIsReady && !force)
            {
                return;
            }

            scriptEditorIsReady = true;
            this._parent = parent;
            ScriptDatabase.InitializePokemonNames();
            ScriptDatabase.InitializeItemNames();
            ScriptDatabase.InitializeMoveNames();
            ScriptDatabase.InitializeTrainerNames();
            SetupScriptEditorTextAreas();

            /* Extract essential NARCs sub-archives*/
            Helpers.statusLabelMessage("Setting up Script Editor...");
            Update();

            DSUtils.TryUnpackNarcs(new List<RomInfo.DirNames> { RomInfo.DirNames.scripts }); //12 = scripts Narc Dir

            populate_selectScriptFileComboBox(0);

            UpdateScriptNumberCheckBox((NumberStyles)SettingsManager.Settings.scriptEditorFormatPreference);
            Helpers.statusLabelMessage();
        }

        public bool OpenScriptEditorAndSave(MainProgram parent, int scriptFileId, bool force = false, bool focus = true)
        {
            try
            {
                OpenScriptEditor(parent, scriptFileId, force, focus);
                SaveSelectedScript(scriptFileId, true);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public void OpenScriptEditor(MainProgram parent, int scriptFileID, bool force = false, bool focus = true)
        {
            SetupScriptEditor(parent, force);

            scriptEditorTabControl.SelectedIndex = 0;
            selectScriptFileComboBox.SelectedIndex = scriptFileID;
            if (!focus) return;
            if (EditorPanels.PopoutRegistry.TryGetHost(this, out var host))
            {
                host.Focus();
            }
            else
            {
                EditorPanels.mainTabControl.SelectedTab = EditorPanels.scriptEditorTabPage;
            }
        }

        private void SetupScriptEditorTextAreas()
        {
            //PREPARE SCRIPT EDITOR KEYWORDS
            cmdKeyWords = String.Join(" ", RomInfo.ScriptCommandNamesDict.Values) +
                          " " + String.Join(" ", ScriptDatabase.movementsDictIDName.Values);

            secondaryKeyWords = String.Join(" ", RomInfo.ScriptComparisonOperatorsDict.Values) +
                                " " + String.Join(" ", ScriptDatabase.specialOverworlds.Values) +
                                " " + String.Join(" ", ScriptDatabase.overworldDirections.Values) +
                                " " + String.Join(" ", ScriptDatabase.pokemonNames.Values) +
                                " " + String.Join(" ", ScriptDatabase.itemNames.Values) +
                                " " + String.Join(" ", ScriptDatabase.moveNames.Values) +
                                " " + String.Join(" ", ScriptDatabase.soundNames.Values) +
                                " " + String.Join(" ", ScriptDatabase.trainerNames.Values) +
                                " " + ScriptFile.ContainerTypes.Script.ToString() +
                                " " + ScriptFile.ContainerTypes.Function.ToString() +
                                " " + ScriptFile.ContainerTypes.Action.ToString() +
                                " " + Event.EventType.Overworld +
                                " " + Overworld.MovementCodeKW;

            altCaseKeywords += " " + cmdKeyWords.ToUpper() + " " + cmdKeyWords.ToLower();
            altCaseKeywords += " " + secondaryKeyWords.ToUpper() + " " + secondaryKeyWords.ToLower();

            // CREATE CONTROLS
            ScriptTextArea = new Scintilla();
            scriptSearchManager = new SearchManager(EditorPanels.MainProgram, ScriptTextArea, panelFindScriptTextBox,
                PanelSearchScripts);
            scintillaScriptsPanel.Controls.Clear();
            scintillaScriptsPanel.Controls.Add(ScriptTextArea);

            FunctionTextArea = new Scintilla();
            functionSearchManager = new SearchManager(EditorPanels.MainProgram, FunctionTextArea,
                panelFindFunctionTextBox, PanelSearchFunctions);
            scintillaFunctionsPanel.Controls.Clear();
            scintillaFunctionsPanel.Controls.Add(FunctionTextArea);

            ActionTextArea = new Scintilla();
            actionSearchManager = new SearchManager(EditorPanels.MainProgram, ActionTextArea, panelFindActionTextBox,
                PanelSearchActions);
            scintillaActionsPanel.Controls.Clear();
            scintillaActionsPanel.Controls.Add(ActionTextArea);

            currentScintillaEditor = ScriptTextArea;
            currentSearchManager = scriptSearchManager;

            // BASIC CONFIG
            ScriptTextArea.TextChanged += (OnTextChangedScript);
            FunctionTextArea.TextChanged += (OnTextChangedFunction);
            ActionTextArea.TextChanged += (OnTextChangedAction);

            ScriptTextArea.CharAdded += OnCharAdded;
            FunctionTextArea.CharAdded += OnCharAdded;
            ActionTextArea.CharAdded += OnCharAdded;

            // INITIAL VIEW CONFIG
            InitialViewConfig(ScriptTextArea);
            InitialViewConfig(FunctionTextArea);
            InitialViewConfig(ActionTextArea);

            InitSyntaxColoring(ScriptTextArea);
            InitSyntaxColoring(FunctionTextArea);
            InitSyntaxColoring(ActionTextArea);

            // NUMBER MARGIN
            InitNumberMargin(ScriptTextArea, ScriptTextArea_MarginClick);
            InitNumberMargin(FunctionTextArea, FunctionTextArea_MarginClick);
            InitNumberMargin(ActionTextArea, ActionTextArea_MarginClick);

            // BOOKMARK MARGIN
            InitBookmarkMargin(ScriptTextArea);
            InitBookmarkMargin(FunctionTextArea);
            InitBookmarkMargin(ActionTextArea);

            // CODE FOLDING MARGIN
            InitCodeFolding(ScriptTextArea);
            InitCodeFolding(FunctionTextArea);
            InitCodeFolding(ActionTextArea);

            // INIT HOTKEYS
            InitHotkeys(ScriptTextArea, scriptSearchManager);
            InitHotkeys(FunctionTextArea, functionSearchManager);
            InitHotkeys(ActionTextArea, actionSearchManager);

            // INIT TOOLTIPS DWELLING
            /*
            ScriptTextArea.MouseDwellTime = 300;
            ScriptTextArea.DwellEnd += TextArea_DwellEnd;
            ScriptTextArea.DwellStart += TextArea_DwellStart;

            FunctionTextArea.MouseDwellTime = 300;
            FunctionTextArea.DwellEnd += TextArea_DwellEnd;
            FunctionTextArea.DwellStart += TextArea_DwellStart;
            */
        }

        private void populate_selectScriptFileComboBox(int selectedIndex = 0)
        {
            selectScriptFileComboBox.Items.Clear();
            int scriptCount = Filesystem.GetScriptCount();
            for (int i = 0; i < scriptCount; i++)
            {
                // ScriptFile currentScriptFile = new ScriptFile(i, true, true);
                // selectScriptFileComboBox.Items.Add(currentScriptFile);
                selectScriptFileComboBox.Items.Add($"Script File {i}");
            }

            selectScriptFileComboBox.SelectedIndex = selectedIndex;
        }

        private void InitialViewConfig(Scintilla textArea)
        {
            textArea.Dock = DockStyle.Fill;
            textArea.WrapMode = WrapMode.Word;
            textArea.IndentationGuides = IndentView.LookBoth;
            textArea.CaretPeriod = 500;
            textArea.CaretForeColor = Color.White;
            textArea.SetSelectionBackColor(true, Color.FromArgb(0x114D9C));
            textArea.WrapIndentMode = WrapIndentMode.Same;

            // Auto Completion
            textArea.AutoCMaxHeight = 20;
            textArea.AutoCIgnoreCase = true;
            textArea.AutoCOrder = Order.Custom;
            textArea.AutoCCancelAtStart = false;
            textArea.AutoCAutoHide = false;
        }

        private void InitSyntaxColoring(Scintilla textArea)
        {
            // Configure the default style
            textArea.StyleResetDefault();
            textArea.Styles[Style.Default].Font = "Consolas";
            textArea.Styles[Style.Default].Size = 12;
            textArea.Styles[Style.Default].BackColor = Color.FromArgb(0x212121);
            textArea.Styles[Style.Default].ForeColor = Color.FromArgb(0xFFFFFF);
            textArea.StyleClearAll();

            // Configure the lexer styles
            textArea.Styles[Style.Python.Identifier].ForeColor = Color.FromArgb(0xD0DAE2);
            textArea.Styles[Style.Python.CommentLine].ForeColor = Color.FromArgb(0x40BF57);
            textArea.Styles[Style.Python.Number].ForeColor = Color.FromArgb(0xFFFF00);
            textArea.Styles[Style.Python.String].ForeColor = Color.FromArgb(0xFF00FF);
            textArea.Styles[Style.Python.Character].ForeColor = Color.FromArgb(0xE95454);
            textArea.Styles[Style.Python.Operator].ForeColor = Color.FromArgb(0xFFFF00);
            textArea.Styles[Style.Python.Word].ForeColor = Color.FromArgb(0x48A8EE);
            textArea.Styles[Style.Python.Word2].ForeColor = Color.FromArgb(0xF98906);

            textArea.Lexer = Lexer.Python;

            textArea.SetKeywords(0, cmdKeyWords);
            textArea.SetKeywords(1, secondaryKeyWords);
            textArea.SetKeywords(2, altCaseKeywords);
        }

        private void InitNumberMargin(Scintilla textArea, EventHandler<MarginClickEventArgs> textArea_MarginClick)
        {
            textArea.Styles[Style.LineNumber].BackColor = BACK_COLOR;
            textArea.Styles[Style.LineNumber].ForeColor = FORE_COLOR;
            textArea.Styles[Style.IndentGuide].ForeColor = FORE_COLOR;
            textArea.Styles[Style.IndentGuide].BackColor = BACK_COLOR;

            Margin nums = textArea.Margins[NUMBER_MARGIN];
            nums.Type = MarginType.Number;
            nums.Sensitive = true;
            nums.Mask = 0;

            textArea.MarginClick += textArea_MarginClick;
        }

        private void InitBookmarkMargin(Scintilla textArea)
        {
            //TextArea.SetFoldMarginColor(true, IntToColor(BACK_COLOR));

            Margin margin = textArea.Margins[BOOKMARK_MARGIN];
            margin.Width = 20;
            margin.Sensitive = true;
            margin.Type = MarginType.Symbol;
            margin.Mask = (1 << BOOKMARK_MARKER);
            //margin.Cursor = MarginCursor.Arrow;

            Marker marker = textArea.Markers[BOOKMARK_MARKER];
            marker.Symbol = MarkerSymbol.Circle;
            marker.SetBackColor(Color.FromArgb(0xFF003B));
            marker.SetForeColor(Color.FromArgb(0x000000));
            marker.SetAlpha(100);
        }

        private void InitCodeFolding(Scintilla textArea)
        {
            textArea.SetFoldMarginColor(true, BACK_COLOR);
            textArea.SetFoldMarginHighlightColor(true, BACK_COLOR);

            // Enable code folding
            textArea.SetProperty("fold", "1");
            textArea.SetProperty("fold.compact", "1");

            // Configure a margin to display folding symbols
            textArea.Margins[FOLDING_MARGIN].Type = MarginType.Symbol;
            textArea.Margins[FOLDING_MARGIN].Mask = Marker.MaskFolders;
            textArea.Margins[FOLDING_MARGIN].Sensitive = true;
            textArea.Margins[FOLDING_MARGIN].Width = 20;

            // Set colors for all folding markers
            for (int i = 25; i <= 31; i++)
            {
                textArea.Markers[i].SetForeColor(BACK_COLOR); // styles for [+] and [-]
                textArea.Markers[i].SetBackColor(FORE_COLOR); // styles for [+] and [-]
            }

            // Configure folding markers with respective symbols
            textArea.Markers[Marker.Folder].Symbol =
                CODE_FOLDING_CIRCULAR ? MarkerSymbol.CirclePlus : MarkerSymbol.BoxPlus;
            textArea.Markers[Marker.FolderOpen].Symbol =
                CODE_FOLDING_CIRCULAR ? MarkerSymbol.CircleMinus : MarkerSymbol.BoxMinus;
            textArea.Markers[Marker.FolderEnd].Symbol = CODE_FOLDING_CIRCULAR
                ? MarkerSymbol.CirclePlusConnected
                : MarkerSymbol.BoxPlusConnected;
            textArea.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
            textArea.Markers[Marker.FolderOpenMid].Symbol = CODE_FOLDING_CIRCULAR
                ? MarkerSymbol.CircleMinusConnected
                : MarkerSymbol.BoxMinusConnected;
            textArea.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
            textArea.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;

            // Enable automatic folding
            textArea.AutomaticFold = (AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change);
        }

        private void InitHotkeys(Scintilla scintillaTb, SearchManager sm)
        {
            // register the hotkeys with the form
            HotKeyManager.AddHotKey(scintillaTb, sm.OpenSearch, Keys.F, true);
            HotKeyManager.AddHotKey(scintillaTb, () => Uppercase(scintillaTb), Keys.U, true);
            HotKeyManager.AddHotKey(scintillaTb, () => Lowercase(scintillaTb), Keys.L, true);
            HotKeyManager.AddHotKey(scintillaTb, () => ZoomIn(scintillaTb), Keys.Oemplus, true);
            HotKeyManager.AddHotKey(scintillaTb, () => ZoomOut(scintillaTb), Keys.OemMinus, true);
            HotKeyManager.AddHotKey(scintillaTb, () => ZoomDefault(scintillaTb), Keys.D0, true);
            HotKeyManager.AddHotKey(scintillaTb, sm.CloseSearch, Keys.Escape);
            HotKeyManager.AddHotKey(scintillaTb, () => ToggleAutoComplete(scintillaTb), Keys.Space, true);
            HotKeyManager.AddHotKey(scintillaTb, () => SaveScriptFile(scintillaTb, false), Keys.S, true);
            HotKeyManager.AddHotKey(scintillaTb, () => FileMovedOnDisk(), Keys.R, true);

            // remove conflicting hotkeys from scintilla
            scintillaTb.ClearCmdKey(Keys.Control | Keys.F);
            scintillaTb.ClearCmdKey(Keys.Control | Keys.Space);
            scintillaTb.ClearCmdKey(Keys.Control | Keys.H);
            scintillaTb.ClearCmdKey(Keys.Control | Keys.L);
            scintillaTb.ClearCmdKey(Keys.Control | Keys.U);
            scintillaTb.ClearCmdKey(Keys.Control | Keys.S);
            scintillaTb.ClearCmdKey(Keys.Control | Keys.R);

            // remove ctrl + space
            scintillaTb.KeyDown += (sender, e) =>
            {
                if (e.Control && e.KeyCode == Keys.Space)
                {
                    e.SuppressKeyPress = true; // Prevents the space from being inserted
                }
            };

        }
        private void Uppercase(Scintilla textArea)
        {
            // save the selection
            int start = textArea.SelectionStart;
            int end = textArea.SelectionEnd;

            // modify the selected text
            textArea.ReplaceSelection(textArea.GetTextRange(start, end - start).ToUpper());

            // preserve the original selection
            textArea.SetSelection(start, end);
        }

        private void Lowercase(Scintilla textArea)
        {
            // save the selection
            int start = textArea.SelectionStart;
            int end = textArea.SelectionEnd;

            // modify the selected text
            textArea.ReplaceSelection(textArea.GetTextRange(start, end - start).ToLower());

            // preserve the original selection
            textArea.SetSelection(start, end);
        }

        private void ZoomIn(Scintilla textArea)
        {
            textArea.ZoomIn();
        }

        private void ZoomOut(Scintilla textArea)
        {
            textArea.ZoomOut();
        }

        private void ZoomDefault(Scintilla textArea)
        {
            textArea.Zoom = 0;
        }

        private void ToggleAutoComplete(Scintilla textArea)
        {
            if (textArea.AutoCActive)
            {
                textArea.AutoCCancel();
                return;
            }
            CompleteCurrent(textArea);
        }

        private void CompleteCurrent(Scintilla textArea)
        {
            int currentPos = textArea.CurrentPosition;
            int wordStartPos = textArea.WordStartPosition(currentPos, true);

            int wordLen = Math.Max(currentPos - wordStartPos, 0);

            string currentWord = textArea.GetTextRange(wordStartPos, wordLen);

            textArea.AutoCShow(wordLen, cmdKeyWords + secondaryKeyWords);
        }

        private void SaveScriptFile(Scintilla textArea, bool showMessage)
        {
            int fileID = currentScriptFile.fileID;
			ScriptTextArea.ReadOnly = true;
            FunctionTextArea.ReadOnly = true;
            ActionTextArea.ReadOnly = true;
            ScriptFile scriptFile = new ScriptFile(ScriptTextArea.Lines.ToStringsList(allowEmpty: true, trim: true), FunctionTextArea.Lines.ToStringsList(allowEmpty: true, trim: true), ActionTextArea.Lines.ToStringsList(allowEmpty: true, trim: true), fileID);
            string path = RomInfo.workDir + "..\\script_export";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string text = ScriptTextArea.Text;
            string text2 = FunctionTextArea.Text;
            string text3 = ActionTextArea.Text;
            string path2 = RomInfo.workDir + "..\\script_export\\" + fileID.ToString("D4") + "_script.script";
            string path3 = RomInfo.workDir + "..\\script_export\\" + fileID.ToString("D4") + "_func.script";
            string path4 = RomInfo.workDir + "..\\script_export\\" + fileID.ToString("D4") + "_action.action";
            WriteAllLinesBetter(path2, text);
            WriteAllLinesBetter(path3, text2);
            WriteAllLinesBetter(path4, text3);
            _ = scriptFile.fileID;
            if (scriptFile.fileID == int.MaxValue)
            {
                MessageBox.Show("This " + typeof(ScriptFile).Name + " is couldn't be saved since it's empty.", "Can't save", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
            {
                scriptFile.SaveToFileDefaultDir(selectScriptFileComboBox.SelectedIndex, showMessage);
                currentScriptFile = scriptFile;
                ScriptEditorSetClean();
            }
            if (scriptFile.hasNoScripts)
            {
                MessageBox.Show("This ScriptFile couldn't be saved. A minimum of one script is required.", "Can't save", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else if (scriptFile.SaveToFileDefaultDir(fileID))
            {
                currentScriptFile = scriptFile;
                ScriptEditorSetClean();
            }

            ScriptTextArea.ReadOnly = false;
            FunctionTextArea.ReadOnly = false;
            ActionTextArea.ReadOnly = false;
        }

        private void ScriptEditorSetClean()
        {
            Helpers.DisableHandlers();

            scriptsTabPage.Text = ScriptFile.ContainerTypes.Script.ToString() + "s";
            functionsTabPage.Text = ScriptFile.ContainerTypes.Function.ToString() + "s";
            actionsTabPage.Text = ScriptFile.ContainerTypes.Action.ToString() + "s";
            scriptsDirty = functionsDirty = actionsDirty = false;

            Helpers.EnableHandlers();
        }

        private void OnTextChangedScript(object sender, EventArgs e)
        {
            ScriptTextArea.Margins[NUMBER_MARGIN].Width = ScriptTextArea.Lines.Count.ToString().Length * 13;

            if (scriptsDirty)
            {
                return;
            }

            scriptsDirty = true;
            scriptsTabPage.Text = ScriptFile.ContainerTypes.Script.ToString() + "s" + "*";
        }

        private void OnTextChangedFunction(object sender, EventArgs e)
        {
            FunctionTextArea.Margins[NUMBER_MARGIN].Width = FunctionTextArea.Lines.Count.ToString().Length * 13;

            if (functionsDirty)
            {
                return;
            }

            functionsDirty = true;
            functionsTabPage.Text = ScriptFile.ContainerTypes.Function.ToString() + "s" + "*";
        }

        private void OnTextChangedAction(object sender, EventArgs e)
        {
            ActionTextArea.Margins[NUMBER_MARGIN].Width = ActionTextArea.Lines.Count.ToString().Length * 13;

            if (actionsDirty)
            {
                return;
            }

            actionsDirty = true;
            actionsTabPage.Text = ScriptFile.ContainerTypes.Action.ToString() + "s" + "*";
        }

        private void OnCharAdded(object sender, EventArgs e)
        {
            if (!(sender is Scintilla textArea))
            {
                return;
            }

            if (!textArea.AutoCActive)
            {
                // If the AutoComplete is active, we don't want to do anything else
                return;
            }

            //CompleteCurrent(textArea);

        }

        private void ScriptTextArea_MarginClick(object sender, MarginClickEventArgs e)
        {
            MarginClick(ScriptTextArea, e);
        }

        private void FunctionTextArea_MarginClick(object sender, MarginClickEventArgs e)
        {
            MarginClick(FunctionTextArea, e);
        }

        private void ActionTextArea_MarginClick(object sender, MarginClickEventArgs e)
        {
            MarginClick(ActionTextArea, e);
        }

        private void MarginClick(Scintilla textArea, MarginClickEventArgs e)
        {
            if (e.Margin == BOOKMARK_MARGIN)
            {
                // Do we have a marker for this line?
                const uint mask = (1 << BOOKMARK_MARKER);
                Line line = textArea.Lines[textArea.LineFromPosition(e.Position)];
                if ((line.MarkerGet() & mask) > 0)
                {
                    // Remove existing bookmark
                    line.MarkerDelete(BOOKMARK_MARKER);
                }
                else
                {
                    // Add bookmark
                    line.MarkerAdd(BOOKMARK_MARKER);
                }
            }
        }

        private void selectScriptFileComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            DisplayScript();
        }

        public void UpdateScriptNumberCheckBox(NumberStyles toSet)
        {
            Helpers.DisableHandlers();
            SettingsManager.Settings.scriptEditorFormatPreference = (int)toSet;

            switch ((NumberStyles)SettingsManager.Settings.scriptEditorFormatPreference)
            {
                case NumberStyles.None:
                    scriptEditorNumberFormatNoPreference.Checked = true;
                    break;
                case NumberStyles.HexNumber:
                    scriptEditorNumberFormatHex.Checked = true;
                    break;
                case NumberStyles.Integer:
                    scriptEditorNumberFormatDecimal.Checked = true;
                    break;
            }

            Console.WriteLine("changed style to " + SettingsManager.Settings.scriptEditorFormatPreference);
            Helpers.EnableHandlers();
        }

        private void UpdateScriptNumberFormat(NumberStyles numberStyle)
        {
            if (Helpers.HandlersEnabled)
            {
                NumberStyles old = (NumberStyles)SettingsManager.Settings.scriptEditorFormatPreference; //Local Backup
                SettingsManager.Settings.scriptEditorFormatPreference = (int)numberStyle;

                if (!DisplayScript())
                {
                    UpdateScriptNumberCheckBox(old); //Restore old checkbox status! Script couldn't be redrawn
                }
            }
        }

        private void UpdateScriptNumberFormatNoPref(object sender, EventArgs e)
        {
            UpdateScriptNumberFormat(NumberStyles.None);
        }

        private void UpdateScriptNumberFormatDec(object sender, EventArgs e)
        {
            UpdateScriptNumberFormat(NumberStyles.Integer);
        }

        private void UpdateScriptNumberFormatHex(object sender, EventArgs e)
        {
            UpdateScriptNumberFormat(NumberStyles.HexNumber);
        }

        private bool DisplayScript()
        {
            Console.WriteLine("Script Reload has been requested");

            /* clear controls */
            if (Helpers.HandlersDisabled || selectScriptFileComboBox.SelectedItem == null)
            {
                return false;
            }

            if (scriptsDirty || functionsDirty || actionsDirty)
            {
                DialogResult d = MessageBox.Show("There are unsaved changes in this Script File.\nDo you wish to discard them?", "Unsaved work", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (!d.Equals(DialogResult.Yes))
                {
                    Helpers.DisableHandlers();
                    // selectScriptFileComboBox.SelectedItem = currentScriptFile;
                    selectScriptFileComboBox.SelectedIndex = (int)currentScriptFile.fileID;
                    Helpers.EnableHandlers();
                    return false;
                }
            }

            Helpers.DisableHandlers();

            ScriptFile lastScriptFile = currentScriptFile;
            // currentScriptFile = (ScriptFile)selectScriptFileComboBox.SelectedItem;
            currentScriptFile = new ScriptFile(selectScriptFileComboBox.SelectedIndex); // Load script file

            ScriptTextArea.ClearAll();
            FunctionTextArea.ClearAll();
            ActionTextArea.ClearAll();

            scriptsNavListbox.Items.Clear();
            functionsNavListbox.Items.Clear();
            actionsNavListbox.Items.Clear();

            //prevent buttons from flickering when the combobox selection changes
            bool typeChanged = true;
            if (lastScriptFile != null)
            {
                typeChanged = lastScriptFile.isLevelScript != currentScriptFile.isLevelScript;
            }

            if (typeChanged)
            {
                if (currentScriptFile.isLevelScript)
                {
                    importScriptFileButton.Location = initial_importScriptFileButton_location;
                    exportScriptFileButton.Location = initial_exportScriptFileButton_location;
                    addScriptFileButton.Location = initial_addScriptFileButton_location;
                    removeScriptFileButton.Location = initial_removeScriptFileButton_location;

                    viewLevelScriptButton.Location = new_viewLevelScript_location;

                    importScriptFileButton.Enabled = false;
                    exportScriptFileButton.Enabled = false;
                    addScriptFileButton.Enabled = false;
                    removeScriptFileButton.Enabled = false;

                    viewLevelScriptButton.Enabled = true;
                }
                else
                {
                    importScriptFileButton.Location = new_importScriptFileButton_location;
                    exportScriptFileButton.Location = new_exportScriptFileButton_location;
                    addScriptFileButton.Location = new_addScriptFileButton_location;
                    removeScriptFileButton.Location = new_removeScriptFileButton_location;

                    viewLevelScriptButton.Location = initial_viewLevelScript_location;

                    importScriptFileButton.Enabled = true;
                    exportScriptFileButton.Enabled = true;
                    addScriptFileButton.Enabled = true;
                    removeScriptFileButton.Enabled = true;

                    viewLevelScriptButton.Enabled = false;
                }
            }

            if (!currentScriptFile.isLevelScript)
            {
                displayScriptFile(ScriptFile.ContainerTypes.Script, currentScriptFile, scriptsNavListbox,
                    ScriptTextArea);
                displayScriptFile(ScriptFile.ContainerTypes.Function, currentScriptFile, functionsNavListbox,
                    FunctionTextArea);
                displayScriptFileActions(ScriptFile.ContainerTypes.Action, currentScriptFile,
                    currentScriptFile.allActions, actionsNavListbox, ActionTextArea);
            }

            ScriptEditorSetClean();

            Helpers.statusLabelMessage();
            Helpers.EnableHandlers();

            return true;
        }

        static void displayScriptFile(ScriptFile.ContainerTypes containerType, ScriptFile currentScriptFile,
            ListBox navListBox, Scintilla textArea)
        {
            if (containerType == ScriptFile.ContainerTypes.Script)
            {
                string path_script = RomInfo.workDir + "..\\script_export\\" + currentScriptFile.fileID.ToString("D4") +
                                     "_script.script";
                textArea.Text = File.ReadAllText(path_script);

                String buffer = "";

                for (int i = 0; i < currentScriptFile.allScripts.Count; i++)
                {
                    ScriptCommandContainer currentScript = currentScriptFile.allScripts[i];
                    /* Write header */
                    string header = ScriptFile.ContainerTypes.Script + " " + (i + 1);
                    buffer += header + ':' + Environment.NewLine;
                    navListBox.Items.Add(header);

                    /* If current script is identical to another, print UseScript instead of commands */
                    if (currentScript.usedScriptID < 0)
                    {
                        for (int j = 0; j < currentScript.commands.Count; j++)
                        {
                            if (!ScriptDatabase.endCodes.Contains(currentScript.commands[j].id))
                            {
                                buffer += '\t';
                            }

                            buffer += currentScript.commands[j].name + Environment.NewLine;
                        }
                    }
                    else
                    {
                        buffer += '\t' + "UseScript_#" + currentScript.usedScriptID + Environment.NewLine;
                    }

                    buffer = "";
                }
            }
            else if (containerType == ScriptFile.ContainerTypes.Function)
            {
                string path_function = RomInfo.workDir + "..\\script_export\\" +
                                       currentScriptFile.fileID.ToString("D4") + "_func.script";
                textArea.Text = File.ReadAllText(path_function);

                String buffer = "";

                for (int i = 0; i < currentScriptFile.allFunctions.Count; i++)
                {
                    ScriptCommandContainer currentFunction = currentScriptFile.allFunctions[i];

                    /* Write Heaader */
                    string header = ScriptFile.ContainerTypes.Function.ToString() + " " + (i + 1);
                    buffer += header + ':' + Environment.NewLine;
                    navListBox.Items.Add(header);

                    /* If current function is identical to a script, print UseScript instead of commands */
                    if (currentFunction.usedScriptID < 0)
                    {
                        for (int j = 0; j < currentFunction.commands.Count; j++)
                        {
                            if (!ScriptDatabase.endCodes.Contains(currentFunction.commands[j].id))
                            {
                                buffer += '\t';
                            }

                            buffer += currentFunction.commands[j].name + Environment.NewLine;
                        }
                    }
                    else
                    {
                        buffer += '\t' + "UseScript_#" + currentFunction.usedScriptID + Environment.NewLine;
                    }

                    buffer = "";
                }
            }
        }

        static void displayScriptFileActions(ScriptFile.ContainerTypes containerType, ScriptFile currentScriptFile,
            List<ScriptActionContainer> commandList, ListBox navListBox, Scintilla textArea)
        {
            /* Add movements */
            string buffer = "";
            string path_action = RomInfo.workDir + "..\\script_export\\" + currentScriptFile.fileID.ToString("D4") +
                                 "_action.action";
            textArea.Text = File.ReadAllText(path_action);

            for (int i = 0; i < commandList.Count; i++)
            {
                ScriptActionContainer currentCommand = commandList[i];

                string header = containerType + " " + (i + 1);
                buffer += header + ':' + Environment.NewLine;
                navListBox.Items.Add(header);

                for (int j = 0; j < currentCommand.commands.Count; j++)
                {
                    ScriptAction command = currentCommand.commands[j];
                    if (!ScriptDatabase.movementEndCodes.Contains(command.id))
                    {
                        buffer += '\t';
                    }

                    buffer += command.name + Environment.NewLine;
                }

                buffer = "";
            }
        }

        private void scriptEditorZoomInButton_Click(object sender, EventArgs e)
        {
            ZoomIn(currentScintillaEditor);
        }

        private void scriptEditorZoomOutButton_Click(object sender, EventArgs e)
        {
            ZoomOut(currentScintillaEditor);
        }

        private void scriptEditorZoomResetButton_Click(object sender, EventArgs e)
        {
            ZoomDefault(currentScintillaEditor);
        }

        private void scriptEditorTabControl_TabIndexChanged(object sender, EventArgs e)
        {
            if (scriptEditorTabControl.SelectedTab == scriptsTabPage)
            {
                currentSearchManager = scriptSearchManager;
                currentScintillaEditor = ScriptTextArea;
            }
            else if (scriptEditorTabControl.SelectedTab == functionsTabPage)
            {
                currentSearchManager = functionSearchManager;
                currentScintillaEditor = FunctionTextArea;
            }
            else
            {
                //Actions
                currentSearchManager = actionSearchManager;
                currentScintillaEditor = ActionTextArea;
            }
        }

        private void removeScriptFileButton_Click(object sender, EventArgs e)
        {
            DialogResult d = MessageBox.Show("Are you sure you want to delete the last Script File?",
                "Confirm deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (d.Equals(DialogResult.Yes))
            {
                /* Delete script file */
                string path = Filesystem.GetScriptPath(selectScriptFileComboBox.Items.Count - 1);
                File.Delete(path);

                /* Check if currently selected file is the last one, and in that case select the one before it */
                int lastIndex = selectScriptFileComboBox.Items.Count - 1;
                if (selectScriptFileComboBox.SelectedIndex == lastIndex)
                {
                    selectScriptFileComboBox.SelectedIndex--;
                }

                /* Remove item from ComboBox */
                selectScriptFileComboBox.Items.RemoveAt(lastIndex);
            }
        }

        private void addScriptFileButton_Click(object sender, EventArgs e)
        {
            /* Add new event file to event folder */
            int fileID = selectScriptFileComboBox.Items.Count;

            ScriptFile scriptFile = new ScriptFile(
                scriptLines: new Scintilla { Text = "Script 1:\nEnd" }.Lines.ToStringsList(trim: true),
                functionLines: null,
                actionLines: null,
                fileID
            );

            //check if ScriptFile instance was created successfully
            if (scriptFile.SaveToFileDefaultDir(fileID, showSuccessMessage: false))
            {
                /* Update ComboBox and select new file */
                selectScriptFileComboBox.Items.Add(scriptFile);
                selectScriptFileComboBox.SelectedItem = scriptFile;
            }
        }

        public static void WriteAllLinesBetter(string path, params string[] lines)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (lines == null)
                throw new ArgumentNullException("lines");

            using (var stream = File.OpenWrite(path))
            {
                stream.SetLength(0);
                using (var writer = new StreamWriter(stream))
                {
                    if (lines.Length > 0)
                    {
                        for (var i = 0; i < lines.Length - 1; i++)
                        {
                            writer.WriteLine(lines[i]);
                        }

                        writer.Write(lines[lines.Length - 1]);
                    }
                }
            }
        }

        private void saveScriptFileButton_Click(object sender, EventArgs e)
        {
            /* Create new ScriptFile object using the values in the script editor */
            SaveSelectedScript(currentScriptFile.fileID);
          
        }

        private void SaveSelectedScript(int fileID, bool silent = false)
        {
              ScriptFile userEdited = new ScriptFile(
                scriptLines: ScriptTextArea.Lines.ToStringsList(trim: true),
                functionLines: FunctionTextArea.Lines.ToStringsList(trim: true),
                actionLines: ActionTextArea.Lines.ToStringsList(trim: true),
                fileID
            );

            DialogResult d = DialogResult.Yes;
            if (!silent)
            {
                d = MessageBox.Show("Do you wish to export the scripts\nin a readable format?", "Unsaved work",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            }
      
            if (d.Equals(DialogResult.Yes))
            {
                string subPath = RomInfo.workDir + "..\\script_export";

                bool exists = System.IO.Directory.Exists(subPath);

                if (!exists)
                    System.IO.Directory.CreateDirectory(subPath);

                string script_lines = ScriptTextArea.Text;
                string function_lines = FunctionTextArea.Text;
                string action_lines = ActionTextArea.Text;
                string path_script = RomInfo.workDir + "..\\script_export\\" + fileID.ToString("D4") + "_script.script";
                string path_function = RomInfo.workDir + "..\\script_export\\" + fileID.ToString("D4") + "_func.script";
                string path_action = RomInfo.workDir + "..\\script_export\\" + fileID.ToString("D4") + "_action.action";

                WriteAllLinesBetter(path_script, script_lines);

                WriteAllLinesBetter(path_function, function_lines);

                WriteAllLinesBetter(path_action, action_lines);

                /* Write new scripts to file after exporting readable*/
                if (userEdited.fileID == null)
                {
                    MessageBox.Show(
                        "This " + typeof(ScriptFile).Name + " couldn't be saved, due to a processing error.",
                        "Can't save", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else if (userEdited.fileID == int.MaxValue)
                {
                    MessageBox.Show("This " + typeof(ScriptFile).Name + " is couldn't be saved since it's empty.",
                        "Can't save", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    //check if ScriptFile instance was created succesfully
                    userEdited.SaveToFileDefaultDir(selectScriptFileComboBox.SelectedIndex, !silent);
                    currentScriptFile = userEdited;
                    ScriptEditorSetClean();
                }
            }

            if (userEdited.hasNoScripts)
            {
                MessageBox.Show(
                    "This " + nameof(ScriptFile) + " couldn't be saved. A minimum of one script is required.",
                    "Can't save", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //check if ScriptFile instance was created successfully
            if (userEdited.SaveToFileDefaultDir(fileID, !silent))
            {
                currentScriptFile = userEdited;
                ScriptEditorSetClean();
            }
        }

        private void exportScriptFileButton_Click(object sender, EventArgs e)
        {
            currentScriptFile.SaveToFileExplorePath(currentScriptFile.ToString(), blindmode: true);
        }

        private void importScriptFileButton_Click(object sender, EventArgs e)
        {
            /* Prompt user to select .scr or .bin file */
            OpenFileDialog of = new OpenFileDialog
            {
                Filter = "Script File (*.scr, *.bin)|*.scr;*.bin"
            };
            if (of.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            /* Update scriptFile object in memory */
            int i = selectScriptFileComboBox.SelectedIndex;
            string path = Filesystem.GetScriptPath(i);
            File.Copy(of.FileName, path, true);

            populate_selectScriptFileComboBox(i);

            /* Refresh controls */
            selectScriptFileComboBox_SelectedIndexChanged(null, null);

            /* Display success message */
            MessageBox.Show("Scripts imported successfully!", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void viewLevelScriptButton_Click(object sender, EventArgs e)
        {
            EditorPanels.levelScriptEditor.OpenLevelScriptEditor(this._parent, selectScriptFileComboBox.SelectedIndex);
        }

        private void locateCurrentScriptFile_Click(object sender, EventArgs e)
        {
            string path = Filesystem.GetScriptPath(selectScriptFileComboBox.SelectedIndex);
            Helpers.ExplorerSelect(path);
        }

        private void findNext(SearchManager searchManager)
        {
            searchManager.Find(true, false);
            scrollResultToTop(searchManager);
        }

        private void findPrev(SearchManager searchManager)
        {
            searchManager.Find(false, false);
            scrollResultToTop(searchManager);
        }

        private void findCurrent(SearchManager searchManager)
        {
            searchManager.Find(true, true);
            scrollResultToTop(searchManager);
        }

        private void TxtFindKeyDown(SearchManager searchManager, KeyEventArgs e)
        {
            if (HotKeyManager.IsHotkey(e, Keys.Enter))
            {
                findNext(searchManager);
            }

            if (HotKeyManager.IsHotkey(e, Keys.Enter, true) || HotKeyManager.IsHotkey(e, Keys.Enter, false, true))
            {
                findPrev(searchManager);
            }
        }

        private void BtnNextFindScript_Click(object sender, EventArgs e)
        {
            findNext(scriptSearchManager);
        }

        private void BtnPrevFindScript_Click(object sender, EventArgs e)
        {
            findPrev(scriptSearchManager);
        }

        private void panelFindScriptTextBox_TextChanged(object sender, EventArgs e)
        {
            findCurrent(scriptSearchManager);
        }

        private void scriptTxtFind_KeyDown(object sender, KeyEventArgs e)
        {
            TxtFindKeyDown(scriptSearchManager, e);
        }

        private void BtnCloseFindScript_Click(object sender, EventArgs e)
        {
            scriptSearchManager.CloseSearch();
        }

        private void BtnNextFindFunc_Click(object sender, EventArgs e)
        {
            findNext(functionSearchManager);
        }

        private void BtnPrevFindFunc_Click(object sender, EventArgs e)
        {
            findNext(functionSearchManager);
        }

        private void panelFindFunctionTextBox_TextChanged(object sender, EventArgs e)
        {
            findNext(functionSearchManager);
        }

        private void functionTxtFind_KeyDown(object sender, KeyEventArgs e)
        {
            TxtFindKeyDown(functionSearchManager, e);
        }

        private void BtnCloseFindFunc_Click(object sender, EventArgs e)
        {
            functionSearchManager.CloseSearch();
        }

        private void BtnNextFindActions_Click(object sender, EventArgs e)
        {
            findNext(actionSearchManager);
        }

        private void BtnPrevFindActions_Click(object sender, EventArgs e)
        {
            findNext(actionSearchManager);
        }

        private void panelFindActionTextBox_TextChanged(object sender, EventArgs e)
        {
            findNext(actionSearchManager);
        }

        private void actionTxtFind_KeyDown(object sender, KeyEventArgs e)
        {
            TxtFindKeyDown(actionSearchManager, e);
        }

        private void BtnCloseFindActions_Click(object sender, EventArgs e)
        {
            actionSearchManager.CloseSearch();
        }

        void scrollResultToTop(SearchManager searchManager)
        {
            int resultStart = searchManager.textAreaScintilla.CurrentLine - ScriptEditorSearchResult.ResultsPadding;
            searchManager.textAreaScintilla.FirstVisibleLine = resultStart;
        }

        private void NavigatorGoTo(ListBox listBox, TabPage tabPage, SearchManager searchManager,
            ScriptFile.ContainerTypes containerType)
        {
            if (listBox.SelectedIndex < 0)
            {
                return;
            }

            scriptEditorTabControl.SelectedTab = tabPage;
            int commandNumber = listBox.SelectedIndex + 1;
            string CommandBlockOpen = $"{containerType} {commandNumber}:";
            searchManager.Find(true, false, CommandBlockOpen);

            scrollResultToTop(searchManager);
        }

        private void scriptsNavListbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            NavigatorGoTo((ListBox)sender, scriptsTabPage, scriptSearchManager, ScriptFile.ContainerTypes.Script);
        }

        private void functionsNavListbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            NavigatorGoTo((ListBox)sender, functionsTabPage, functionSearchManager, ScriptFile.ContainerTypes.Function);
        }

        private void actionsNavListbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            NavigatorGoTo((ListBox)sender, actionsTabPage, actionSearchManager, ScriptFile.ContainerTypes.Action);
        }

        private void openFindScriptEditorButton_Click(object sender, EventArgs e)
        {
            currentSearchManager.OpenSearch();
        }

        private void ScriptEditorExpandButton_Click(object sender, EventArgs e)
        {
            currentScintillaEditor.FoldAll(FoldAction.Expand);
        }

        private void ScriptEditorCollapseButton_Click(object sender, EventArgs e)
        {
            currentScintillaEditor.FoldAll(FoldAction.Contract);
        }

        private void scriptEditorWordWrapCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            ScriptTextArea.WrapMode = scriptEditorWordWrapCheckbox.Checked ? WrapMode.Word : WrapMode.None;
            FunctionTextArea.WrapMode = scriptEditorWordWrapCheckbox.Checked ? WrapMode.Word : WrapMode.None;
            ActionTextArea.WrapMode = scriptEditorWordWrapCheckbox.Checked ? WrapMode.Word : WrapMode.None;
        }

        private void viewWhiteSpacesButton_Click(object sender, EventArgs e)
        {
            ScriptTextArea.ViewWhitespace = scriptEditorWhitespacesCheckbox.Checked
                ? WhitespaceMode.VisibleAlways
                : WhitespaceMode.Invisible;
            FunctionTextArea.ViewWhitespace = scriptEditorWhitespacesCheckbox.Checked
                ? WhitespaceMode.VisibleAlways
                : WhitespaceMode.Invisible;
            ActionTextArea.ViewWhitespace = scriptEditorWhitespacesCheckbox.Checked
                ? WhitespaceMode.VisibleAlways
                : WhitespaceMode.Invisible;
        }

        private void searchInScriptsTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                searchInScriptsButton_Click(null, null);
            }
        }

        public List<ScriptFile> getScriptsToSearch()
        {
            List<ScriptFile> scriptsToSearch = new List<ScriptFile>();

            if (searchOnlyCurrentScriptCheckBox.Checked)
            {
                this.UIThread(() => { searchProgressBar.Maximum = 1; });
                int i = selectScriptFileComboBox.SelectedIndex;
                ScriptFile scriptFile = new ScriptFile(i);
                Console.WriteLine("Attempting to load script " + scriptFile.fileID);
                scriptsToSearch.Add(scriptFile);
                this.UIThread(() => { searchProgressBar.IncrementNoAnimation(); });
            }
            else
            {
                this.UIThread(() => { searchProgressBar.Maximum = selectScriptFileComboBox.Items.Count; });
                for (int i = 0; i < selectScriptFileComboBox.Items.Count; i++)
                {
                    ScriptFile scriptFile = new ScriptFile(i);
                    Console.WriteLine("Attempting to load script " + scriptFile.fileID);
                    scriptsToSearch.Add(scriptFile);
                    this.UIThread(() => { searchProgressBar.IncrementNoAnimation(); });
                }
            }

            return scriptsToSearch;
        }

        private void searchInScriptsButton_Click(object sender, EventArgs e)
        {
            if (searchInScriptsTextBox.Text == "")
            {
                return;
            }

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (_sender, args) =>
            {
                this.UIThread(() =>
                {
                    searchInScriptsResultListBox.Items.Clear();
                    searchProgressBar.Value = 0;
                });

                List<ScriptFile> scriptsToSearch = getScriptsToSearch();

                string searchString = searchInScriptsTextBox.Text;
                Func<string, bool> searchCriteriaCS =
                    (string s) => s.IndexOf(searchString, StringComparison.InvariantCulture) >= 0;
                Func<string, bool> searchCriteriaCI = (string s) =>
                    s.IndexOf(searchString, StringComparison.InvariantCultureIgnoreCase) >= 0;
                Func<string, bool> searchCriteria =
                    scriptSearchCaseSensitiveCheckBox.Checked ? searchCriteriaCS : searchCriteriaCI;

                List<ScriptEditorSearchResult> results = new List<ScriptEditorSearchResult>();
                foreach (ScriptFile scriptFile in scriptsToSearch)
                {
                    List<ScriptEditorSearchResult> scriptResults =
                        SearchInScripts(scriptFile, scriptFile.allScripts, searchCriteria);
                    List<ScriptEditorSearchResult> functionResults =
                        SearchInScripts(scriptFile, scriptFile.allFunctions, searchCriteria);
                    // List<ScriptEditorSearchResult> actionResults = SearchInScripts(scriptFile, scriptFile.allActions, searchCriteria);
                    results.AddRange(scriptResults);
                    results.AddRange(functionResults);
                    // results.AddRange(actionResults);
                }

                this.UIThread(() =>
                {
                    searchInScriptsResultListBox.Items.AddRange(results.ToArray());
                    searchProgressBar.Value = 0;
                });
            };

            bw.RunWorkerAsync();
        }

        private List<ScriptEditorSearchResult> SearchInScripts(ScriptFile scriptFile,
            List<ScriptCommandContainer> commandContainers, Func<string, bool> criteria)
        {
            List<ScriptEditorSearchResult> results = new List<ScriptEditorSearchResult>();

            for (int j = 0; j < commandContainers.Count; j++)
            {
                if (commandContainers[j].commands is null)
                {
                    continue;
                }

                ScriptCommandContainer scriptCommandContainer = commandContainers[j];
                foreach (ScriptCommand scriptCommand in scriptCommandContainer.commands)
                {
                    if (criteria(scriptCommand.name))
                    {
                        results.Add(new ScriptEditorSearchResult(scriptFile, scriptCommandContainer.containerType,
                            j + 1, scriptCommand));
                    }
                }
            }

            return results;
        }

        private void searchInScriptsResultListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                goToSearchResult();
            }
        }

        private void searchInScripts_GoToEntryResult(object sender, MouseEventArgs e)
        {
            goToSearchResult();
        }

        private void goToSearchResult()
        {
            if (searchInScriptsResultListBox.SelectedItem == null)
            {
                return;
            }

            ScriptEditorSearchResult searchResult = (ScriptEditorSearchResult)searchInScriptsResultListBox.SelectedItem;
            ScriptFile scriptFile = searchResult.scriptFile;
            ScriptFile.ContainerTypes containerType = searchResult.containerType;

            selectScriptFileComboBox.SelectedIndex = scriptFile.fileID;

            if (containerType == ScriptFile.ContainerTypes.Script)
            {
                displaySearchResult(scriptsTabPage, scriptSearchManager, searchResult);
            }
            else if (containerType == ScriptFile.ContainerTypes.Function)
            {
                displaySearchResult(functionsTabPage, functionSearchManager, searchResult);
            }
            else if (containerType == ScriptFile.ContainerTypes.Action)
            {
                displaySearchResult(actionsTabPage, actionSearchManager, searchResult);
            }
        }

        private void displaySearchResult(TabPage tabPage, SearchManager searchManager,
            ScriptEditorSearchResult searchResult)
        {
            if (scriptEditorTabControl.SelectedTab != tabPage)
            {
                scriptEditorTabControl.SelectedTab = tabPage;
            }

            searchManager.Find(true, false, searchResult.CommandBlockOpen);
            int blockStart = searchManager.textAreaScintilla.CurrentLine - ScriptEditorSearchResult.ResultsPadding;

            searchManager.Find(true, false, searchResult.scriptCommand.name);
            int resultStart = searchManager.textAreaScintilla.CurrentLine - ScriptEditorSearchResult.ResultsPadding;

            if (scrollToBlockStartcheckBox.Checked)
            {
                searchManager.textAreaScintilla.FirstVisibleLine = blockStart;
            }
            else
            {
                searchManager.textAreaScintilla.FirstVisibleLine = resultStart;
            }
        }

        private void exportallcustom_button_Click(object sender, EventArgs e)
        {
            int scriptCount = Filesystem.GetScriptCount();

            for (int i = 0; i < scriptCount; i++)
            {
                int fileID = i;

                ScriptFile userEdited = new ScriptFile(
                    scriptLines: ScriptTextArea.Lines.ToStringsList(trim: true),
                    functionLines: FunctionTextArea.Lines.ToStringsList(trim: true),
                    actionLines: ActionTextArea.Lines.ToStringsList(trim: true),
                    fileID
                );

                string subPath = RomInfo.workDir + "..\\script_export";

                bool exists = System.IO.Directory.Exists(subPath);

                if (!exists)
                    System.IO.Directory.CreateDirectory(subPath);

                string script_lines = ScriptTextArea.Text;
                string function_lines = FunctionTextArea.Text;
                string action_lines = ActionTextArea.Text;
                string path_script = RomInfo.workDir + "..\\script_export\\" + fileID.ToString("D4") + "_script.script";
                string path_function = RomInfo.workDir + "..\\script_export\\" + fileID.ToString("D4") + "_func.script";
                string path_action = RomInfo.workDir + "..\\script_export\\" + fileID.ToString("D4") + "_action.action";

                WriteAllLinesBetter(path_script, script_lines);
                WriteAllLinesBetter(path_function, function_lines);
                WriteAllLinesBetter(path_action, action_lines);

                if (selectScriptFileComboBox.SelectedIndex <= scriptCount)
                {
                    selectScriptFileComboBox.SelectedIndex = selectScriptFileComboBox.SelectedIndex + 1;
                }
            }
        }

        private void openInVSC_Click(object sender, EventArgs e)
        {
            {
                string filePath = Filesystem.GetExportedScriptPath(selectScriptFileComboBox.SelectedIndex);
                string scriptsPath = RomInfo.exportedScriptsPath;

                string fullFilePath = Path.GetFullPath(filePath);
                string fullFolderPath = Path.GetFullPath(scriptsPath);

                string arguments = $"\"{fullFolderPath}\" \"{fullFilePath}\"";

                StartProcess(SettingsManager.Settings.vscPath, arguments);
            }
        }

        static void StartProcess(string exePath, string arguments)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments,
                    UseShellExecute = true
                };
                Process.Start(psi);
                Console.WriteLine($"Opened: {arguments}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening: {arguments}\n{ex.Message}");
            }
        }

        bool ContainersHaveSameCommands(ScriptCommandContainer current, ScriptCommandContainer disk)
        {
            if (current.commands.Count != disk.commands.Count)
                return false;

            for (int i = 0; i < current.commands.Count; i++)
            {
                var cmdCurrent = current.commands[i];
                var cmdDisk = disk.commands[i];

                // Compare field by field — adjust to your ScriptCommand structure
                if (cmdCurrent.name != cmdDisk.name)
                    return false;

                if (cmdCurrent.cmdParams.Count != cmdDisk.cmdParams.Count)
                    return false;

                for (int j = 0; j < cmdCurrent.cmdParams.Count; j++)
                {
                    string a = cmdCurrent.cmdParams[j].ToString() ?? string.Empty;
                    string b = cmdDisk.cmdParams[j]?.ToString() ?? string.Empty;

                    if (!string.Equals(a, b, StringComparison.Ordinal))
                        return false;
                }
            }

            return true;
        }
        
        bool ActionContainersHaveSameCommands(ScriptActionContainer current, ScriptActionContainer disk)
        {
            if (current.commands.Count != disk.commands.Count)
                return false;

            for (int i = 0; i < current.commands.Count; i++)
            {
                var cmdCurrent = current.commands[i];
                var cmdDisk = disk.commands[i];

                // Compare field by field — adjust to your ScriptCommand structure
                if (cmdCurrent.name != cmdDisk.name)
                    return false;
            }

            return true;
        }


        public void FileMovedOnDisk()
        {
            ScriptTextArea.ReadOnly = true;
            FunctionTextArea.ReadOnly = true;
            ActionTextArea.ReadOnly = true;

            int fileID = (int)currentScriptFile.fileID;
            AppLogger.Info("Comparing opened  files with the ones on disk");
            string path_script = RomInfo.workDir + "..\\script_export\\" + fileID.ToString("D4") + "_script.script";
            string path_function = RomInfo.workDir + "..\\script_export\\" + fileID.ToString("D4") + "_func.script";
            string path_action = RomInfo.workDir + "..\\script_export\\" + fileID.ToString("D4") + "_action.action";
            
            ScriptFile currentLines = new ScriptFile(
                scriptLines: ScriptTextArea.Lines.ToStringsList(trim: true),
                functionLines: FunctionTextArea.Lines.ToStringsList(trim: true),
                actionLines: ActionTextArea.Lines.ToStringsList(trim: true),
                fileID
            );

            ScriptFile outsideLines = new ScriptFile(
                scriptLines: File.ReadLines(path_script).Select(line => line.Trim()).ToList(),
                functionLines: File.ReadLines(path_function).Select(line => line.Trim()).ToList(),
                actionLines: File.ReadLines(path_action).Select(line => line.Trim()).ToList(),
                fileID
            );
            
            bool scriptsEqual = true;
            
            for (int i = 0; i < currentLines.allScripts.Count; i++)
            {
                if (!ContainersHaveSameCommands(currentLines.allScripts[i], outsideLines.allScripts[i]))
                {
                    scriptsEqual = false;
                    break;
                }
            }
            
            for (int i = 0; i < currentLines.allFunctions.Count; i++)
            {
                if (!ContainersHaveSameCommands(currentLines.allFunctions[i], outsideLines.allFunctions[i]))
                {
                    scriptsEqual = false;
                    break;
                }
            }
            
            for (int i = 0; i < currentLines.allActions.Count; i++)
            {
                if (!ActionContainersHaveSameCommands(currentLines.allActions[i], outsideLines.allActions[i]))
                {
                    scriptsEqual = false;
                    break;
                }
            }

            if (!scriptsEqual)
            {
                AppLogger.Info("The script file has been modified outside of the editor.");
                DialogResult result =
                    MessageBox.Show(
                        "The script file has been modified outside of the editor. Do you want to reload it?",
                        "Script File Modified", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    ScriptTextArea.ReadOnly = false;
                    FunctionTextArea.ReadOnly = false;
                    ActionTextArea.ReadOnly = false;
                    DisplayScript();
                }
            }
            else
            {
                AppLogger.Info("No changes detected in the script file.");
            }

            ScriptTextArea.ReadOnly = false;
            FunctionTextArea.ReadOnly = false;
            ActionTextArea.ReadOnly = false;


        }
    }


    public class ScriptEditorSearchResult
    {
        public readonly ScriptFile scriptFile;
        public readonly ScriptFile.ContainerTypes containerType;
        public readonly int commandNumber;
        public readonly ScriptCommand scriptCommand;

        public const int ResultsPadding = 1;

        public ScriptEditorSearchResult(ScriptFile scriptFile, ScriptFile.ContainerTypes containerType,
            int commandNumber, ScriptCommand scriptCommand)
        {
            this.scriptFile = scriptFile;
            this.containerType = containerType;
            this.commandNumber = commandNumber;
            this.scriptCommand = scriptCommand;
        }

        public string CommandBlockOpen
        {
            get { return $"{containerType} {commandNumber}:"; }
        }

        public override string ToString()
        {
            return $"File {scriptFile.fileID} - {CommandBlockOpen} {scriptCommand.name}";
        }
    }
}