using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using LiTRE.Editors;
using LiTRE.ROMFiles;

namespace LiTRE
{
    public static class IpcEvents
    {
        public static IpcResponse saveScriptIpcHandler(int id, string path, MainProgram parent)
        {
            bool opened = EditorPanels.scriptEditor.OpenScriptEditorAndSave(parent, (int)id, true, false);
            if(opened) 
                return IpcResponse.Success();
            else
                return IpcResponse.Fail($"Error while saving script {id}");
        }

        public static IpcResponse openRelatedEditors(int id, string type,  MainProgram parent) 
        {
            var intNames = EditorPanels.headerEditor.internalNames;
            var results = HeaderSearch.AdvancedSearch(0, (ushort)intNames.Count, intNames, (int)MapHeader.SearchableFields.ScriptFileID, 0, id.ToString())
                .Select(x => ushort.Parse(x.Split()[0]))
                .ToArray();
            HeaderPt hpt;
            hpt = (HeaderPt)MapHeader.LoadFromFile(RomInfo.gameDirs[RomInfo.DirNames.dynamicHeaders].unpackedDir + "\\" + results[0].ToString("D4"), results[0], 0);
            switch (type)
            {
                case "text":
                    EditorPanels.textEditor.OpenTextEditor(parent, hpt.textArchiveID, EditorPanels.headerEditor.locationNameComboBox);
                    break;
                case "event":
                    EditorPanels.eventEditor.SetupEventEditor(parent);


                    if (EditorPanels.headerEditor.matrixUpDown.Value != 0)
                    {
                        EditorPanels.eventEditor.eventAreaDataUpDown.Value = EditorPanels.headerEditor.areaDataUpDown.Value; // Use Area Data for textures if matrix is not 0
                    }

                    EditorPanels.eventEditor.eventMatrixUpDown.Value = EditorPanels.headerEditor.matrixUpDown.Value; // Open the right matrix in event editor
                    EditorPanels.eventEditor.selectEventComboBox.SelectedIndex = hpt.eventFileID;
                    if (EditorPanels.PopoutRegistry.TryGetHost(EditorPanels.eventEditor, out var host))
                    {
                        host.Focus();
                    }
                    else
                    {
                        parent.mainTabControl.SelectedTab = EditorPanels.eventEditorTabPage;
                    }

                    EditorPanels.eventEditor.eventMatrixUpDown_ValueChanged(null, null);
                    break;
                case "lvscript":
                    EditorPanels.levelScriptEditor.OpenLevelScriptEditor(parent, hpt.levelScriptID);
                    break;
            }
            
            return IpcResponse.Success();
        }

        public class OwImage
        {
            public int OwID { get; set; }
            public byte[] Image { get; set; }
        } 
        public class EventFileAndImages
        {
            public EventFile eventFile;
            public List<OwImage> imageList;
        }
        
        public static EventFileAndImages _getEventData(int id, MainProgram parent) 
        {
            EditorPanels.eventEditor.SetupEventEditor(parent);
            
            var intNames = EditorPanels.headerEditor.internalNames;
            var results = HeaderSearch.AdvancedSearch(0, (ushort)intNames.Count, intNames, (int)MapHeader.SearchableFields.ScriptFileID, 0, id.ToString())
                .Select(x => ushort.Parse(x.Split()[0]))
                .ToArray();
            HeaderPt hpt;
            hpt = (HeaderPt)MapHeader.LoadFromFile(RomInfo.gameDirs[RomInfo.DirNames.dynamicHeaders].unpackedDir + "\\" + results[0].ToString("D4"), results[0], 0);
            var eventFile = new EventFile(hpt.eventFileID);
            var imageList = new List<OwImage>();
            if (eventFile.overworlds.Count > 0)
            {
                foreach (var overworld in eventFile.overworlds)
                {
                    var image = EventEditorLiT.GetOverworldImage(overworld.overlayTableEntry, overworld.orientation);
                    byte[] byteImage = null;
                    using (var stream = new MemoryStream())
                    {
                        image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        byteImage = stream.ToArray();
                    }

                    var owImage = new OwImage()
                    {
                        Image = byteImage,
                        OwID = overworld.owID
                    };
                    imageList.Add(owImage);
                }
            }
            
            return new EventFileAndImages()
            {
                eventFile = eventFile,
                imageList = imageList
            };
            
        }

        public static TextArchive getArchive(int id, MainProgram parent)
        {
            EditorPanels.eventEditor.SetupEventEditor(parent);
            
            var intNames = EditorPanels.headerEditor.internalNames;
            var results = HeaderSearch.AdvancedSearch(0, (ushort)intNames.Count, intNames, (int)MapHeader.SearchableFields.ScriptFileID, 0, id.ToString())
                .Select(x => ushort.Parse(x.Split()[0]))
                .ToArray();
            HeaderPt hpt;
            hpt = (HeaderPt)MapHeader.LoadFromFile(RomInfo.gameDirs[RomInfo.DirNames.dynamicHeaders].unpackedDir + "\\" + results[0].ToString("D4"), results[0], 0);
            return new TextArchive(hpt.textArchiveID);
        }
    }
}