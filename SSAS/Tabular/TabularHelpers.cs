using System;
using Extensibility;
using EnvDTE;
using EnvDTE80;
using System.Xml;
using Microsoft.VisualStudio.CommandBars;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;
using Microsoft.AnalysisServices;

namespace BIDSHelper
{
    public class TabularHelpers
    {
        private static Microsoft.AnalysisServices.VSHost.VSHostManager GetVSHostManager(UIHierarchyItem hierItem, bool openIfNotOpen)
        {
            Microsoft.VisualStudio.Project.Automation.OAFileItem project = hierItem.Object as Microsoft.VisualStudio.Project.Automation.OAFileItem;
            if (project == null) return null;
            return GetVSHostManager(project, openIfNotOpen);
        }

        private static Microsoft.AnalysisServices.VSHost.VSHostManager GetVSHostManager(Microsoft.VisualStudio.Project.Automation.OAFileItem project, bool openIfNotOpen)
        {
            if (project == null) return null;
            if (openIfNotOpen && !project.get_IsOpen(EnvDTE.Constants.vsViewKindPrimary))
            {
                Window win = project.Open(EnvDTE.Constants.vsViewKindPrimary);
                if (win == null) throw new Exception("BIDS Helper was unable to open designer window.");
                win.Activate();
            }

            Microsoft.AnalysisServices.VSHost.Integration.BISMFileNode bim = project.Object as Microsoft.AnalysisServices.VSHost.Integration.BISMFileNode;
            if (bim == null) return null;
            Microsoft.AnalysisServices.VSHost.VSHostManager host = (Microsoft.AnalysisServices.VSHost.VSHostManager)bim.GetType().InvokeMember("VSHostManager", System.Reflection.BindingFlags.ExactBinding | System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, bim, null); //bim.VSHostManager;
            return host;
        }

        public static IServiceProvider GetTabularServiceProviderFromBimFile(UIHierarchyItem hierItem, bool openIfNotOpen)
        {
            GetVSHostManager(hierItem, openIfNotOpen); //just to force the window open if not open
            return ((Microsoft.VisualStudio.Project.ProjectNode)((((Microsoft.VisualStudio.Project.Automation.OAProjectItem<Microsoft.VisualStudio.Project.FileNode>)(hierItem.Object)).ContainingProject).Object)).Site;
        }

        public static IServiceProvider GetTabularServiceProviderFromActiveWindow()
        {
            return GetTabularServiceProviderFromProjectItem(Connect.Application.ActiveWindow.ProjectItem, false);
        }

        public static IServiceProvider GetTabularServiceProviderFromProjectItem(ProjectItem projectItem, bool openIfNotOpen)
        {
            Microsoft.VisualStudio.Project.Automation.OAFileItem project = projectItem as Microsoft.VisualStudio.Project.Automation.OAFileItem;
            GetVSHostManager(project, openIfNotOpen); //just to force the window open if not open
            return ((Microsoft.VisualStudio.Project.ProjectNode)((project.ContainingProject).Object)).Site;
        }

        public static Microsoft.AnalysisServices.BackEnd.DataModelingSandbox GetTabularSandboxFromBimFile(UIHierarchyItem hierItem, bool openIfNotOpen)
        {
            Microsoft.AnalysisServices.VSHost.VSHostManager host = GetVSHostManager(hierItem, openIfNotOpen);
            if (host == null) return null;
            return host.Sandbox;
        }

        public static Microsoft.AnalysisServices.BackEnd.DataModelingSandbox GetTabularSandboxFromActiveWindow()
        {
            return GetTabularSandboxFromProjectItem(Connect.Application.ActiveWindow.ProjectItem, false);
        }

        public static Microsoft.AnalysisServices.BackEnd.DataModelingSandbox GetTabularSandboxFromProjectItem(ProjectItem projectItem, bool openIfNotOpen)
        {
            Microsoft.VisualStudio.Project.Automation.OAFileItem project = projectItem as Microsoft.VisualStudio.Project.Automation.OAFileItem;
            Microsoft.AnalysisServices.VSHost.VSHostManager host = GetVSHostManager(project, openIfNotOpen);
            if (host == null) return null;
            return host.Sandbox;
        }

        public static Microsoft.AnalysisServices.Common.SandboxEditor GetTabularSandboxEditorFromBimFile(UIHierarchyItem hierItem, bool openIfNotOpen)
        {
            Microsoft.AnalysisServices.VSHost.VSHostManager host = GetVSHostManager(hierItem, openIfNotOpen);
            if (host == null) return null;
            return host.Editor;
        }

        public static Microsoft.AnalysisServices.Common.SandboxEditor GetTabularSandboxEditorFromActiveWindow()
        {
            return GetTabularSandboxEditorFromProjectItem(Connect.Application.ActiveWindow.ProjectItem, false);
        }

        public static Microsoft.AnalysisServices.Common.SandboxEditor GetTabularSandboxEditorFromProjectItem(ProjectItem projectItem, bool openIfNotOpen)
        {
            Microsoft.VisualStudio.Project.Automation.OAFileItem project = projectItem as Microsoft.VisualStudio.Project.Automation.OAFileItem;
            Microsoft.AnalysisServices.VSHost.VSHostManager host = GetVSHostManager(project, openIfNotOpen);
            if (host == null) return null;
            return host.Editor;
        }

        public static Microsoft.AnalysisServices.Common.ERDiagram GetTabularERDiagramFromSandboxEditor(Microsoft.AnalysisServices.Common.SandboxEditor editor)
        {
            Microsoft.AnalysisServices.Common.DiagramDisplay diagramDisplay = (Microsoft.AnalysisServices.Common.DiagramDisplay)editor.GetType().InvokeMember("GetCurrentDiagramDisplay", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.NonPublic, null, editor, new object[] { });
            Microsoft.AnalysisServices.Common.ERDiagram diagram = null;
            if (diagramDisplay != null)
            {
                diagram = diagramDisplay.Diagram as Microsoft.AnalysisServices.Common.ERDiagram;
            }
            return diagram;
        }

        public static void SaveXmlAnnotation(MajorObject obj, string annotationName, object annotationValue)
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(annotationValue.GetType());
            StringBuilder sb = new StringBuilder();
            System.IO.StringWriter writer = new System.IO.StringWriter(sb);
            serializer.Serialize(writer, annotationValue);
            System.Xml.XmlDocument xml = new XmlDocument();
            xml.LoadXml(sb.ToString());
            if (obj.Annotations.Contains(annotationName)) obj.Annotations.Remove(annotationName);
            if (TabularAnnotationWorkaroundPlugin.AreAnnotationsStringStyle(obj))
            {
                //this is just a workaround to this bug: https://connect.microsoft.com/SQLServer/feedback/details/776444/tabular-model-error-during-opening-bim-after-sp1-readelementcontentas-methods-cannot-be-called-on-an-element-that-has-child-elements
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.OmitXmlDeclaration = true;
                StringBuilder sb2 = new StringBuilder();
                XmlWriter wr = XmlWriter.Create(sb2, settings);
                XmlDocument annotationDoc = new XmlDocument();
                annotationDoc.LoadXml(xml.InnerXml);
                annotationDoc.Save(wr);
                wr.Close();

                Annotation annotation = new Annotation(annotationName, sb2.ToString());
                obj.Annotations.Add(annotation);
            }
            else
            {
                Annotation annotation = annotation = new Annotation(annotationName, xml.DocumentElement);
                obj.Annotations.Add(annotation);
            }
        }

        public static string GetAnnotationXml(MajorObject obj, string annotationName)
        {
            if (TabularAnnotationWorkaroundPlugin.AreAnnotationsStringStyle(obj))
            {
                return obj.Annotations[annotationName].Value.InnerText;
            }
            else
            {
                return obj.Annotations[annotationName].Value.OuterXml;
            }
        }

        //executes the commands that we have scripted using AMO previously
        public static void ExecuteCaptureLog(Server server, bool parallel, bool useTransaction)
        {
            //execute XMLA
            XmlaResultCollection oPrepResults = server.ExecuteCaptureLog(useTransaction, parallel);
            server.CaptureLog.Clear();

            string sErrors = string.Empty;
            string sWarnings = string.Empty;

            //Check for errors. If there are any, mark the processing as failed and capture errors.
            foreach (XmlaResult oPrepResult in oPrepResults)
            {
                foreach (XmlaMessage oPrepMessage in oPrepResult.Messages)
                {
                    if (oPrepMessage is XmlaError)
                    {
                        XmlaError oError = (XmlaError)oPrepMessage;
                        sErrors += "ERROR " + oError.ErrorCode + " - " + oPrepMessage.Description + Environment.NewLine;
                    }
                    else if (oPrepMessage is XmlaWarning)
                    {
                        XmlaWarning oWarning = (XmlaWarning)oPrepMessage;
                        sWarnings += "WARNING " + oWarning.WarningCode + " - " + oPrepMessage.Description + Environment.NewLine;
                    }
                    else
                    {
                        sWarnings += "WARNING - " + oPrepMessage.Description + Environment.NewLine;
                    }
                }
            }

            if (!string.IsNullOrEmpty(sErrors))
            {
                throw new Exception(sErrors + sWarnings);
            }
        }

        /// <summary>
        /// Will ensure that the SSDT has the credentials (SQL auth and impersonation info) so that when we alter the whole database object it won't wipe out the data source credentials
        /// </summary>
        /// <param name="sandbox"></param>
        public static bool EnsureDataSourceCredentials(Microsoft.AnalysisServices.BackEnd.DataModelingSandbox sandbox)
        {
            Database db = sandbox.Database;
            foreach (DataSource ds in db.DataSources)
            {
                if (!Microsoft.AnalysisServices.Common.CommonFunctions.HandlePasswordPrompt(null, sandbox, ds.ID, null))
                {
                    return false;
                }
                if (!Microsoft.AnalysisServices.Common.CommonFunctions.HandlePasswordPromptForImpersonation(null, sandbox, ds.ID, null))
                {
                    return false;
                }
            }
            return true;
        }
    }
}

