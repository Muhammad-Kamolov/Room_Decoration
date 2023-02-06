using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;
using System.Linq;

namespace Room_Decoration
{
    [Transaction(TransactionMode.Manual)]
    public class Room_Decoration : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Form1 frm = new Form1(commandData);
            //frm.ShowDialog();
            // TaskDialog.Show("t","Room_Decoration.cs is Working");
            
            
            Reference refer = uidoc.Selection.PickObject(ObjectType.Element);
            Wall mainWall = doc.GetElement(refer) as Wall;
            TaskDialog.Show("t", mainWall.GetMaterialIds(false).FirstOrDefault().ToString()  + "  -  " + mainWall.GetMaterialIds(false).Count.ToString());

            return Result.Succeeded;
        }        

    }
}
