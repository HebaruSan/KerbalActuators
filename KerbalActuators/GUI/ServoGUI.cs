﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using KSP.UI.Screens;

/*
Source code copyrighgt 2018, by Michael Billard (Angel-125)
License: GNU General Public License Version 3
License URL: http://www.gnu.org/licenses/
If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace KerbalActuators
{
    public enum EEditStates
    {
        None,
        MoveUp,
        MoveDown,
        Delete,
        EditMode
    }

    public class ServoGUI : Window<ServoGUI>
    {
        public const int BASE_HEIGHT = 100;
        public const string kDirections1 = "<color=white>Create new position </color>";
        public const string kDirections2 = "<color=white>Select position to update</color>";
        public const string kDirections3 = "<color=white>Update position data</color>";
        public const string kDirections4 = "<color=white>Move position up</color>";
        public const string kDirections5 = "<color=white>Move position down</color>";
        public const string kDirections6 = "<color=white>Play position</color>";
        public const string kDirections7 = "<color=white>Delete position</color>";
        public const string kSavedFile = " saved to disk.";
        public const string kConfirmOverwrite = "<color=yellow>File exists, overwrite?</color>";
        public static Texture homeIcon = null;
        public static Texture recordIcon = null;
        public static Texture cameraIcon = null;
        public static Texture upIcon = null;
        public static Texture downIcon = null;
        public static Texture playIcon = null;
        public static Texture deleteIcon = null;
        public static Texture okIcon = null;
        public static Texture cancelIcon = null;
        public static Texture newIcon = null;
        public static Texture editIcon = null;
        public static Texture helpIcon = null;
        public static Texture loadIcon = null;
        public static Texture saveIcon = null;
        public static Texture newHomeIcon = null;
        public static Texture stopIcon = null;
        public static Texture minIcon = null;
        public static Texture maxIcon = null;
        public static Texture forwardIcon = null;
        public static Texture backIcon = null;
        public static GUILayoutOption[] buttonOptions = new GUILayoutOption[] { GUILayout.Width(32), GUILayout.Height(32) };

        public IServoController[] servoControllers;
        public int maxWindowHeight = 600;
        public List<ConfigNode> sequences = new List<ConfigNode>();
        public WBIServoManager servoManager;

        Vector2 scrollPos;
        Vector2 playbookScrollPos;
        Vector2 recorderScrollPos;
        int panelHeight;
        GUILayoutOption[] sequencePanelOptions = new GUILayoutOption[] { GUILayout.Width(150) };
        GUILayoutOption[] servoPanelOptions = new GUILayoutOption[] { GUILayout.Width(275) };
        GUILayoutOption[] stopButtonOptions = new GUILayoutOption[] { GUILayout.Width(60), GUILayout.Height(60) };
        GUILayoutOption[] okCancelOptions = new GUILayoutOption[] { GUILayout.Width(32), GUILayout.Height(32) };
        string sequenceName = "Sequence";
        string positionName = "Position";
        List<ConfigNode> snapshots = new List<ConfigNode>();
        bool recorderIsVisible = false;
        bool directionsVisible = false;
        ConfigNode pendingSnapshotUpdate = null;
        int editIndexPos = -1;
        EEditStates editStatePos = EEditStates.None;
        ConfigNode pendingSequenceNode = null;
        int editIndexSeq = -1;
        string saveFolder;
        string saveSequencePath;
        int saveSequenceIndex;
        bool drawConfirmationDialog;
        bool drawSequenceLoader;
        List<string> sequenceFiles = new List<string>();

        public ServoGUI(string title = "", int height = 400, int width = 500) :
            base(title, width, height)
        {
            scrollPos = new Vector2(0, 0);
            playbookScrollPos = new Vector2(0, 0);
            recorderScrollPos = new Vector2(0, 0);

        }

        public override void SetVisible(bool newValue)
        {
            base.SetVisible(newValue);

            //Grab the textures if needed.
            if (homeIcon == null)
            {
                string baseIconURL = WBIServoManager.ICON_PATH;
                ConfigNode settingsNode = GameDatabase.Instance.GetConfigNode("KerbalActuators");
                if (settingsNode != null)
                    baseIconURL = settingsNode.GetValue("iconsFolder");
                homeIcon = GameDatabase.Instance.GetTexture(baseIconURL + "House", false);
                recordIcon = GameDatabase.Instance.GetTexture(baseIconURL + "Record", false);
                cameraIcon = GameDatabase.Instance.GetTexture(baseIconURL + "Camera", false);
                upIcon = GameDatabase.Instance.GetTexture(baseIconURL + "LeftArrow", false);
                downIcon = GameDatabase.Instance.GetTexture(baseIconURL + "RightArrow", false);
                playIcon = GameDatabase.Instance.GetTexture(baseIconURL + "PlayIcon", false);
                deleteIcon = GameDatabase.Instance.GetTexture(baseIconURL + "TrashCan", false);
                okIcon = GameDatabase.Instance.GetTexture(baseIconURL + "WBIOK", false);
                cancelIcon = GameDatabase.Instance.GetTexture(baseIconURL + "Cancel", false);
                newIcon = GameDatabase.Instance.GetTexture(baseIconURL + "NewIcon", false);
                editIcon = GameDatabase.Instance.GetTexture(baseIconURL + "EditIcon", false);
                helpIcon = GameDatabase.Instance.GetTexture(baseIconURL + "HelpIcon", false);
                loadIcon = GameDatabase.Instance.GetTexture(baseIconURL + "Load", false);
                saveIcon = GameDatabase.Instance.GetTexture(baseIconURL + "Save", false);
                newHomeIcon = GameDatabase.Instance.GetTexture(baseIconURL + "NewHome", false);
                stopIcon = GameDatabase.Instance.GetTexture(baseIconURL + "Stop", false);
                minIcon = GameDatabase.Instance.GetTexture(baseIconURL + "Min", false);
                maxIcon = GameDatabase.Instance.GetTexture(baseIconURL + "Max", false);
                forwardIcon = GameDatabase.Instance.GetTexture(baseIconURL + "Forward", false);
                backIcon = GameDatabase.Instance.GetTexture(baseIconURL + "Backward", false);
            }

            //Determine the save folder & sequence files
            getSaveFolder();
            getSequenceFiles();

            if (newValue)
            {
                panelHeight = 0;
                for (int index = 0; index < servoControllers.Length; index++)
                {
                    panelHeight += servoControllers[index].GetPanelHeight();
                }

                if (panelHeight > maxWindowHeight)
                    panelHeight = maxWindowHeight;

                windowPos.height = panelHeight;
            }
        }

        protected void getSequenceFiles()
        {
            string[] folderFiles = Directory.GetFiles(saveFolder);
            sequenceFiles.Clear();
            for (int index = 0; index < folderFiles.Length; index++)
            {
                if (folderFiles[index].Contains(servoManager.part.name))
                    sequenceFiles.Add(folderFiles[index]);
            }
        }

        protected void getSaveFolder()
        {
            // See: http://stackoverflow.com/questions/52797/how-do-i-get-the-path-of-the-assembly-the-code-is-in/283917#283917
            DirectoryInfo directoryInfo = Directory.GetParent(Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).Path))).Parent.Parent;
            string codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            string fullPath = Path.GetDirectoryName(path);

            //Setup the root folder and thumbs folder.
            if (fullPath.Contains("GameData"))
            {
                int index = fullPath.IndexOf("GameData");
                fullPath = fullPath.Substring(0, index);
                saveFolder = fullPath + "saves/" + HighLogic.SaveFolder + "/";
            }
            Debug.Log("[ServoGUI] - saveFolder: " + saveFolder);
        }

        protected override void DrawWindowContents(int windowId)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();

            //Confirmation dialog
            if (drawConfirmationDialog)
            {
                drawConfirmationPanel();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                return;
            }

            //Draw sequence playbook or load sequence or recorder
            if (drawSequenceLoader)
                drawSequenceLoaderPanel();
            else if (recorderIsVisible)
                drawRecorder();
            else
                drawPlaybook();

            //Draw servo controllers
            drawServoControllers();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        protected void drawConfirmationPanel()
        {
            GUILayout.BeginVertical();
            GUILayout.Label(kConfirmOverwrite);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(okIcon, okCancelOptions))
            {
                drawConfirmationDialog = false;
                sequences[saveSequenceIndex].Save(saveSequencePath);
                ScreenMessages.PostScreenMessage(saveSequencePath + kSavedFile, 6.0f, ScreenMessageStyle.UPPER_CENTER);
                getSequenceFiles();
            }
            if (GUILayout.Button(cancelIcon, okCancelOptions))
            {
                drawConfirmationDialog = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            //Draw servo controllers
            drawServoControllers();
        }

        protected void drawSequenceLoaderPanel()
        {
            //List of sequences
            GUILayout.BeginVertical();
            scrollPos = GUILayout.BeginScrollView(scrollPos, servoPanelOptions);

            int totalFiles = sequenceFiles.Count;
            string fileName;
            int nameStartIndex;
            for (int index = 0; index < totalFiles; index++)
            {
                nameStartIndex = sequenceFiles[index].LastIndexOf("/") + 1;
                fileName = sequenceFiles[index].Substring(nameStartIndex);
                if (GUILayout.Button(fileName))
                {
                    drawSequenceLoader = false;
                    ConfigNode sequenceNode = ConfigNode.Load(sequenceFiles[index]);
                    sequenceNode.name = WBIServoManager.SEQUENCE_NODE;
                    servoManager.AddSequence(sequenceNode);
                }
            }

            GUILayout.EndScrollView();

            //Cancel button
            if (GUILayout.Button(cancelIcon, okCancelOptions))
                drawSequenceLoader = false;
            GUILayout.EndVertical();
        }

        protected void drawDirections()
        {
            GUILayout.BeginVertical();

            //Create new
            GUILayout.BeginHorizontal();
            GUILayout.Label(newIcon, buttonOptions);
            GUILayout.Label(kDirections1);
            GUILayout.EndHorizontal();

            //Select existing
            GUILayout.BeginHorizontal();
            GUILayout.Label(editIcon, buttonOptions);
            GUILayout.Label(kDirections2);
            GUILayout.EndHorizontal();

            //Update existing
            GUILayout.BeginHorizontal();
            GUILayout.Label(cameraIcon, buttonOptions);
            GUILayout.Label(kDirections3);
            GUILayout.EndHorizontal();

            //Position up
            GUILayout.BeginHorizontal();
            GUILayout.Label(upIcon, buttonOptions);
            GUILayout.Label(kDirections4);
            GUILayout.EndHorizontal();

            //Position down
            GUILayout.BeginHorizontal();
            GUILayout.Label(downIcon, buttonOptions);
            GUILayout.Label(kDirections5);
            GUILayout.EndHorizontal();

            //Delete existing
            GUILayout.BeginHorizontal();
            GUILayout.Label(playIcon, buttonOptions);
            GUILayout.Label(kDirections6);
            GUILayout.EndHorizontal();

            //Delete existing
            GUILayout.BeginHorizontal();
            GUILayout.Label(deleteIcon, buttonOptions);
            GUILayout.Label(kDirections7);
            GUILayout.EndHorizontal();

            if (GUILayout.Button(okIcon, okCancelOptions))
                directionsVisible = false;

            GUILayout.EndVertical();
        }

        protected void drawPositionItemNormalMode(int index)
        {
            //Label
            GUILayout.Label("<color=white>" + snapshots[index].GetValue("name") + "</color>");

            GUILayout.BeginHorizontal();

            //Up arrow
            if (index > 0)
            {
                if (GUILayout.Button(upIcon, buttonOptions))
                {
                    editIndexPos = index;
                    editStatePos = EEditStates.MoveUp;
                }
            }

            //Down arrow
            if (index < snapshots.Count - 1)
            {
                if (GUILayout.Button(downIcon, buttonOptions))
                {
                    editIndexPos = index;
                    editStatePos = EEditStates.MoveDown;
                }
            }

            //Play
            if (GUILayout.Button(playIcon, buttonOptions))
            {
                servoManager.PlaySnapshot(snapshots[index]);
            }

            //Edit
            if (GUILayout.Button(editIcon, buttonOptions))
            {
                positionName = snapshots[index].GetValue("name");
                editIndexPos = index;
                editStatePos = EEditStates.EditMode;
            }

            //Delete
            if (GUILayout.Button(deleteIcon, buttonOptions))
            {
                editIndexPos = index;
                editStatePos = EEditStates.Delete;
            }

            GUILayout.EndHorizontal();
        }

        protected void drawPositionItemEditMode(int index)
        {
            //If this isn't the item we're editing then just show the label.
            if (index != editIndexPos)
            {
                GUILayout.Label("<color=white>" + snapshots[index].GetValue("name") + "</color>");
                return;
            }

            //Position name
            GUILayout.Label("<b><color=white>Name:</color></b>");
            positionName = GUILayout.TextField(positionName);

            GUILayout.BeginHorizontal();

            //Update
            if (GUILayout.Button(cameraIcon, buttonOptions))
            {
                pendingSnapshotUpdate = servoManager.TakeSnapshot();
                pendingSnapshotUpdate.AddValue("name", positionName);
            }

            //Play
            if (GUILayout.Button(playIcon, buttonOptions))
            {
                if (pendingSnapshotUpdate != null)
                    servoManager.PlaySnapshot(pendingSnapshotUpdate);
                else
                    servoManager.PlaySnapshot(snapshots[index]);
            }

            //OK
            if (GUILayout.Button(okIcon, buttonOptions))
            {
                if (pendingSnapshotUpdate != null)
                {
                    pendingSnapshotUpdate.SetValue("name", positionName);
                    snapshots[editIndexPos] = pendingSnapshotUpdate;
                }

                else
                {
                    snapshots[editIndexPos].SetValue("name", positionName);
                }

                editStatePos = EEditStates.None;
                pendingSnapshotUpdate = null;
            }

            //Cancel
            if (GUILayout.Button(cancelIcon, buttonOptions))
            {
                editStatePos = EEditStates.None;
                pendingSnapshotUpdate = null;
            }

            GUILayout.EndHorizontal();
        }

        protected void drawRecorder()
        {
            ConfigNode curNode = null;

            //Draw directions if need be.
            if (directionsVisible)
            {
                drawDirections();
                return;
            }

            GUILayout.BeginVertical();

            //Sequence name
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b><color=white>Name:</color></b>");
            sequenceName = GUILayout.TextField(sequenceName);

            //Play sequence button
            if (GUILayout.Button(playIcon, buttonOptions))
                servoManager.PlaySnapshot(snapshots);

            GUILayout.EndHorizontal();

            //Positions
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b><color=white>Positions</color></b>");

            //New snapshot button
            if (GUILayout.Button(newIcon, buttonOptions))
            {
                ConfigNode snapshotNode = servoManager.TakeSnapshot();

                snapshots.Add(snapshotNode);

                snapshotNode.AddValue("name", "Position" + snapshots.Count);
            }

            //Home button: set all controllers to their home position
            if (GUILayout.Button(homeIcon, buttonOptions))
            {
                //First item in the sequences list is the home sequence.
                if (sequences.Count >= 1)
                    servoManager.PlayHomeSequence();
            }

            //Stop all servo movement
            if (GUILayout.Button(stopIcon, stopButtonOptions))
            {
                servoManager.StopAllServos();
            }

            GUILayout.EndHorizontal();

            //Snapshots list
            recorderScrollPos = GUILayout.BeginScrollView(recorderScrollPos);
            for (int index = 0; index < snapshots.Count; index++)
            {
                if (editStatePos != EEditStates.EditMode)
                    drawPositionItemNormalMode(index);
                else
                    drawPositionItemEditMode(index);
            }

            //Handle index repositioning and deletion events
            switch (editStatePos)
            {
                case EEditStates.MoveUp:
                    curNode = snapshots[editIndexPos];
                    snapshots[editIndexPos] = snapshots[editIndexPos - 1];
                    snapshots[editIndexPos - 1] = curNode;
                    editIndexPos = -1;
                    editStatePos = EEditStates.None;
                    break;

                case EEditStates.MoveDown:
                    curNode = snapshots[editIndexPos];
                    snapshots[editIndexPos] = snapshots[editIndexPos + 1];
                    snapshots[editIndexPos + 1] = curNode;
                    editIndexPos = -1;
                    editStatePos = EEditStates.None;
                    break;

                case EEditStates.Delete:
                    snapshots.RemoveAt(editIndexPos);
                    editIndexPos = -1;
                    editStatePos = EEditStates.None;
                    break;

                default:
                    break;
            }

            GUILayout.EndScrollView();

            //OK & Cancel buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(okIcon, okCancelOptions))
            {
                recorderIsVisible = false;

                //Set the name
                pendingSequenceNode.SetValue("name", sequenceName);

                //Add the snapshots
                if (pendingSequenceNode.HasNode(WBIServoManager.SNAPSHOT_NODE))
                    pendingSequenceNode.RemoveNodes(WBIServoManager.SNAPSHOT_NODE);
                foreach (ConfigNode snapshotNode in snapshots)
                    pendingSequenceNode.AddNode(snapshotNode);

                //Either add a new sequence or update the selected one
                if (editIndexSeq == -1)
                    sequences.Add(pendingSequenceNode);
                else
                    sequences[editIndexSeq] = pendingSequenceNode;

                //Cleanup
                editIndexSeq = -1;
            }

            if (GUILayout.Button(cancelIcon, okCancelOptions))
                recorderIsVisible = false;

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(helpIcon, okCancelOptions))
                directionsVisible = true;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        protected void drawPlaybook()
        {
            int deleteIndex = -1;

            //Home & Record buttons
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            //Home button: set all controllers to their home position
            if (GUILayout.Button(homeIcon, buttonOptions))
            {
                //First item in the sequences list is the home sequence.
                if (sequences.Count >= 1)
                    servoManager.PlaySequence(0);
            }

            //Record and edit a new sequence
            if (GUILayout.Button(recordIcon, buttonOptions))
            {
                recorderIsVisible = true;
                editIndexSeq = -1;
                pendingSequenceNode = new ConfigNode(WBIServoManager.SEQUENCE_NODE);
                sequenceName = string.Format("Sequence{0:n0}", sequences.Count - 1);
                pendingSequenceNode.AddValue("name", sequenceName);
                pendingSequenceNode.AddValue("partName", servoManager.part.name);
            }

            //Load a new sequece
            if (GUILayout.Button(loadIcon, buttonOptions))
            {
                drawSequenceLoader = true;
            }

            //New home sequence
            if (GUILayout.Button(newHomeIcon, buttonOptions))
            {
                ConfigNode homeSequence = null;
                ConfigNode snapshot = null;
                snapshot = new ConfigNode(WBIServoManager.SNAPSHOT_NODE);
                homeSequence = new ConfigNode(WBIServoManager.SEQUENCE_NODE);
                homeSequence.AddValue("name", WBIServoManager.kHomeSequenceName);
                homeSequence.AddValue("partName", servoManager.part.name);
                homeSequence.AddNode(snapshot);

                //Take a snapshot
                for (int index = 0; index < servoControllers.Length; index++)
                    snapshot.AddNode(servoControllers[index].TakeSnapshot());

                //Set the new home sequence
                servoManager.CreateHomeSequence(homeSequence);
            }

            //Stop all servo movement
            if (GUILayout.Button(stopIcon, stopButtonOptions))
            {
                servoManager.StopAllServos();
            }

            GUILayout.EndHorizontal();

            GUILayout.Label("<b><color=white>Sequences</color></b>");

            //list of recorded sequences
            playbookScrollPos = GUILayout.BeginScrollView(playbookScrollPos);

            //We skip the first sequence, which is always the home sequence.
            int totalSequences = sequences.Count;
            if (totalSequences > 1)
            {
                for (int index = 1; index < totalSequences; index++)
                {
                    GUILayout.BeginHorizontal();

                    //Play sequence
                    if (GUILayout.Button(sequences[index].GetValue("name")))
                        servoManager.PlaySequence(index);

                    //Edit sequence
                    if (GUILayout.Button(editIcon, buttonOptions))
                    {
                        recorderIsVisible = true;
                        editIndexSeq = index;
                        pendingSequenceNode = sequences[index];
                        snapshots.Clear();
                        snapshots.AddRange(pendingSequenceNode.GetNodes(WBIServoManager.SNAPSHOT_NODE));
                    }

                    //Delete sequence
                    if (GUILayout.Button(deleteIcon, buttonOptions))
                        deleteIndex = index;

                    //Save sequence
                    if (GUILayout.Button(saveIcon, buttonOptions))
                    {
                        saveSequencePath = saveFolder + servoManager.part.name + "_" + sequences[index].GetValue("name") + ".txt";
                        saveSequenceIndex = index;

                        if (File.Exists(saveSequencePath))
                        {
                            drawConfirmationDialog = true;
                        }

                        else
                        {
                            sequences[index].Save(saveSequencePath);
                            ScreenMessages.PostScreenMessage(saveSequencePath + kSavedFile, 6.0f, ScreenMessageStyle.UPPER_CENTER);
                            getSequenceFiles();
                        }
                    }

                    GUILayout.EndHorizontal();
                }

                //Do we have a sequence marked for death?
                if (deleteIndex != -1)
                {
                    sequences.RemoveAt(deleteIndex);
                    deleteIndex = -1;
                }
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        protected void drawServoControllers()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos, servoPanelOptions);

            for (int index = 0; index < servoControllers.Length; index++)
            {
                servoControllers[index].DrawControls();
            }

            GUILayout.EndScrollView();
        }
    }
}
