using System.Collections.Generic;

namespace DSPRE.ROMFiles {
  public class ScriptActionContainer {
    public List<ScriptAction> commands =  new List<ScriptAction>();
    public uint manualUserID;

    public ScriptActionContainer(uint actionNumber, List<ScriptAction> commands = null) {
      manualUserID = actionNumber;
      if(commands==null)
        commands = new List<ScriptAction>();
      this.commands = commands;
    }
  }
}
