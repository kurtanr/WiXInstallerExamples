using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Windows.Forms;

namespace _07_MyCSharpCustomAction
{
  public class MyCustomActions
  {
    [CustomAction]
    public static ActionResult MyImmediateCustomAction(Session session)
    {
      // This logging is visible in MsiExec log file
      session.Log("Begin MyImmediateCustomAction");

      // In immediate custom action, WiX property values can be accessed directly (using session indexer)
      // CustomActionData.ToString() is empty and accessing a property using CustomActionData indexer throws exception
      CustomActionData customActionData = session.CustomActionData;

      string message =
        $"customActionData = '{customActionData}'{Environment.NewLine}{Environment.NewLine}" +
        $"customActionData[\"MY_PROPERTY\"] -> throws KeyNotFoundException{Environment.NewLine}" +
        $"'The given key was not present in the dictionary.'{Environment.NewLine}{Environment.NewLine}" +
        $"session[\"MY_PROPERTY\"] = '{session["MY_PROPERTY"]}'";

      // System.Diagnostics.Debugger.Launch();
      MessageBox.Show(message, "Immediate mode custom action", MessageBoxButtons.OK, MessageBoxIcon.Information);

      session.Log($"MY_PROPERTY value in MyImmediateCustomAction = {session["MY_PROPERTY"]}");
      session.Log($"MY_INSTALL_LOCATION value in MyImmediateCustomAction = {session["MY_INSTALL_LOCATION"]}");
      session.Log("End MyImmediateCustomAction");

      return ActionResult.Success;
    }

    [CustomAction]
    public static ActionResult MyDeferredCustomAction(Session session)
    {
      // This logging is visible in MsiExec log file
      session.Log("Begin MyDeferredCustomAction");

      // In deferred custom action, WiX property values can only be accessed through CustomActionData
      // Accessing property value using session indexer throws exception
      CustomActionData customActionData = session.CustomActionData;

      string message =
        $"customActionData = '{customActionData}'{Environment.NewLine}{Environment.NewLine}" +
        $"customActionData[\"MY_PROPERTY\"] = '{customActionData["MY_PROPERTY"]}'{Environment.NewLine}{Environment.NewLine}" +
        $"session[\"MY_PROPERTY\"] -> throws InstallerException{Environment.NewLine}" +
        "'Cannot access session details from a non-immediate custom action'";

      // System.Diagnostics.Debugger.Launch();
      MessageBox.Show(message, "Deferred mode custom action", MessageBoxButtons.OK, MessageBoxIcon.Information);

      session.Log($"MY_PROPERTY value in MyDeferredCustomAction = {customActionData["MY_PROPERTY"]}");
      session.Log($"MY_INSTALL_LOCATION value in MyDeferredCustomAction = {customActionData["MY_INSTALL_LOCATION"]}");
      session.Log("End MyDeferredCustomAction");

      return ActionResult.Success;
    }
  }
}