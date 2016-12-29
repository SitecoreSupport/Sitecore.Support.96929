namespace Sitecore.Support.Shell.Applications.Workbox
{
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Exceptions;
    using Sitecore.Globalization;
    using Sitecore.Pipelines;
    using Sitecore.Text;
    using Sitecore.Web;
    using Sitecore.Web.UI.Sheer;
    using Sitecore.Workflows;
    using Sitecore.Workflows.Simple;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class WorkboxForm : Sitecore.Shell.Applications.Workbox.WorkboxForm
    {
        private OffsetCollection Offset;

        private class OffsetCollection
        {
            public int this[string key]
            {
                get
                {
                    int num2;
                    if (Context.ClientPage.ServerProperties[key] != null)
                    {
                        return (int)Context.ClientPage.ServerProperties[key];
                    }
                    UrlString str = new UrlString(WebUtil.GetRawUrl());
                    if (str[key] == null)
                    {
                        return 0;
                    }
                    if (!int.TryParse(str[key], out num2))
                    {
                        return 0;
                    }
                    return num2;
                }
                set
                {
                    Context.ClientPage.ServerProperties[key] = value;
                }
            }
        }

        public WorkboxForm()
        {
            this.Offset = new OffsetCollection();
        }

        public new void Comment(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull((object)args, "args");
            ID result1 = ID.Null;
            if (Context.ClientPage.ServerProperties["command"] != null)
                ID.TryParse(Context.ClientPage.ServerProperties["command"] as string, out result1);
            ItemUri itemUri = new ItemUri((Context.ClientPage.ServerProperties["id"] ?? (object)string.Empty).ToString(), Language.Parse(Context.ClientPage.ServerProperties["language"] as string), Sitecore.Data.Version.Parse(Context.ClientPage.ServerProperties["version"] as string), Context.ContentDatabase);
            bool flag = args.Parameters["ui"] != null && args.Parameters["ui"] == "1" || args.Parameters["suppresscomment"] != null && args.Parameters["suppresscomment"] == "1";
            if (!args.IsPostBack && result1 != (ID)null && !flag)
            {
                WorkflowUIHelper.DisplayCommentDialog(itemUri, result1);
                args.WaitForPostBack();
            }
            else if (args.Result != null && args.Result.Length > 2000)
            {
                Context.ClientPage.ClientResponse.ShowError(new Exception(string.Format("The comment is too long.\n\nYou have entered {0} characters.\nA comment cannot contain more than 2000 characters.", (object)args.Result.Length)));
                WorkflowUIHelper.DisplayCommentDialog(itemUri, result1);
                args.WaitForPostBack();
            }
            else
            {
                if ((args.Result == null || !(args.Result != "null") || (!(args.Result != "undefined") || !(args.Result != "cancel"))) && !flag)
                    return;
                string result2 = args.Result;
                Sitecore.Collections.StringDictionary commentFields = string.IsNullOrEmpty(result2) ? new Sitecore.Collections.StringDictionary() : WorkflowUIHelper.ExtractFieldsFromFieldEditor(result2);
                try
                {
                    IWorkflow workflowFromPage = this.GetWorkflowFromPage();
                    if (workflowFromPage == null)
                        return;
                    Item obj = Database.GetItem(itemUri);
                    if (obj == null)
                        return;
                    Processor completionCallback = new Processor("Workflow complete state item count", (object)this, "SupportWorkflowCompleteStateItemCount");
                    WorkflowUIHelper.ExecuteCommand(obj, workflowFromPage, Context.ClientPage.ServerProperties["command"] as string, commentFields, completionCallback);
                }
                catch (WorkflowStateMissingException ex)
                {
                    SheerResponse.Alert("One or more items could not be processed because their workflow state does not specify the next step.");
                }
            }
        }

        private IWorkflow GetWorkflowFromPage()
        {
            IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
            if (workflowProvider == null)
            {
                return null;
            }
            return workflowProvider.GetWorkflow(Context.ClientPage.ServerProperties["workflowid"] as string);
        }

        [UsedImplicitly]
        private void WorkflowCompleteRefresh(WorkflowPipelineArgs args)
        {
            this.Refresh();
        }

        [UsedImplicitly]
        private void SupportWorkflowCompleteStateItemCount(WorkflowPipelineArgs args)
        {
            IWorkflow workflowFromPage = this.GetWorkflowFromPage();
            if (workflowFromPage != null)
            {
                int itemCount = workflowFromPage.GetItemCount(args.PreviousState.StateID);
                if ((this.PageSize > 0) && ((itemCount % this.PageSize) == 0))
                {
                    if ((itemCount / this.PageSize) > 1)
                    {
                        #region Original code
                        // this.Offset[args.PreviousState.StateID]--; 
                        #endregion
                        #region Modified code
                        if (this.Offset[args.PreviousState.StateID] != 0)
                        {
                            this.Offset[args.PreviousState.StateID] = this.Offset[args.PreviousState.StateID]--;
                        } 
                        #endregion
                    }
                    else
                    {
                        this.Offset[args.PreviousState.StateID] = 0;
                    }
                }
                Dictionary<string, string> urlArguments = workflowFromPage.GetStates().ToDictionary<WorkflowState, string, string>(state => state.StateID, state => this.Offset[state.StateID].ToString());
                this.Refresh(urlArguments);
            }
        }
    }
}
