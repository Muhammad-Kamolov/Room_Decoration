using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
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
            Document doc = uiapp.ActiveUIDocument.Document;
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Form1 frm = new Form1(commandData);
            frm.ShowDialog();

           // TaskDialog.Show("t","Room_Decoration.cs is Working");

            #region
            //ViewPlan vp = doc.ActiveView as ViewPlan;
            //if (vp == null)
            //{
            //    TaskDialog.Show("t", "Данный вид не является ViewPlan");
            //}
            //else
            //{
            //    //TaskDialog.Show("t", "Выберите тип стены");
            //    //Reference refer = uidoc.Selection.PickObject(ObjectType.Element);

            //    WallType wallType = (new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Walls).WhereElementIsElementType().
            //                 Where(w => w.Name.Equals("ADSK_Отделка_Условная_20")).FirstOrDefault() as WallType);

            //    ElementId wallType_Id = wallType.Id;
            //    double wallType_width = wallType.Width;

            //    // 
            //    FilteredElementCollector rooms_test = new FilteredElementCollector(doc, doc.ActiveView.Id).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType();
            //    List<Room> rooms = new List<Room>();
            //    foreach (Element item in rooms_test)
            //    {
            //        rooms.Add(item as Room);
            //    }
            //    //

            //    //// Для выборки по мыши (тест)
            //    //TaskDialog.Show("t", "Выберите комнату");
            //    //Reference refer_Room = uidoc.Selection.PickObject(ObjectType.Element);
            //    //Room room = doc.GetElement(refer_Room) as Room;
            //    //


            //    SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions();
            //    opt.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish;


            //    foreach (Room room in rooms)
            //    {
            //        List<BoundarySegment> boundarySegments_list_Final = new List<BoundarySegment>();

            //        // Фильтрация списка
            //        foreach (List<BoundarySegment> boundarySegments_list in room.GetBoundarySegments(opt).ToList())
            //        {
            //            foreach (BoundarySegment item in boundarySegments_list)
            //            {
            //                if (doc.GetElement(item.ElementId) != null)
            //                {
            //                    boundarySegments_list_Final.Add(item);
            //                }
            //            }
            //        }

            //        // Создание Отделки для стен
            //        using (Transaction tx = new Transaction(doc, "Test"))
            //        {
            //            tx.Start("Transaction Start");
            //            for (int i = 0; i < boundarySegments_list_Final.Count; i++)
            //            {
            //                if (doc.GetElement(boundarySegments_list_Final[i].ElementId).Category.Name == "Стены")
            //                {
            //                    // Создаю Отделку для основных стен
            //                    Curve curve = boundarySegments_list_Final[i].GetCurve();
            //                    Curve curve_offset = curve.CreateOffset(wallType_width * (-1) / 2, new XYZ(0, 0, 1));
            //                    Wall wall = doc.GetElement(boundarySegments_list_Final[i].ElementId) as Wall;
            //                    ElementId levelId = wall.LevelId;

            //                    Wall.Create(doc,
            //                       curve_offset,
            //                       wallType_Id,
            //                       levelId,
            //                       4,
            //                       0,
            //                       false,
            //                       false);


            //                    // Проверка торцов (Создаю отделку для торцевых частей)
            //                    Wall myWall;
            //                    Wall firstWall;
            //                    Wall secondWall;
            //                    Curve curve_2_offset;
            //                    if (i != 0 && (boundarySegments_list_Final[i].GetCurve().GetEndPoint(0).X != boundarySegments_list_Final[i - 1].GetCurve().GetEndPoint(1).X ||
            //                        boundarySegments_list_Final[i].GetCurve().GetEndPoint(0).Y != boundarySegments_list_Final[i - 1].GetCurve().GetEndPoint(1).Y))
            //                    {
            //                        Line curve_2;
            //                        try
            //                        {
            //                            curve_2 = Line.CreateBound(boundarySegments_list_Final[i].GetCurve().GetEndPoint(0), boundarySegments_list_Final[i - 1].GetCurve().GetEndPoint(1));
            //                        }
            //                        catch (Autodesk.Revit.Exceptions.ArgumentsInconsistentException ex)
            //                        {
            //                            goto Found;
            //                        }

            //                        firstWall = doc.GetElement(boundarySegments_list_Final[i].ElementId) as Wall; // Начальная точка курва
            //                        secondWall = doc.GetElement(boundarySegments_list_Final[i - 1].ElementId) as Wall; // Конечная точка курва

            //                        // Если отделка вокруг одной стены (с разных сторон в одной помещении)
            //                        if ((firstWall != null && secondWall != null) && (firstWall.Id == secondWall.Id))
            //                        {
            //                            curve_2_offset = curve_2.CreateOffset(wallType_width / 2, new XYZ(0, 0, 1));

            //                            myWall = Wall.Create(doc,
            //                               curve_2_offset,
            //                               wallType_Id,
            //                               levelId,
            //                               5,
            //                               0,
            //                               false,
            //                               false);


            //                            //// Соединяю торцовую отделку
            //                            WallUtils.AllowWallJoinAtEnd(myWall, 0);
            //                            WallUtils.AllowWallJoinAtEnd(myWall, 1);
            //                        }
            //                        else
            //                        {
            //                            curve_2_offset = curve_2.CreateOffset(wallType_width / 2, new XYZ(0, 0, 1));

            //                            myWall = Wall.Create(doc,
            //                               curve_2_offset,
            //                               wallType_Id,
            //                               levelId,
            //                               5,
            //                               0,
            //                               false,
            //                               false);

            //                            // Отсоединяю торцовую отделку, от двух сторон (потом соединю с одной)
            //                            WallUtils.DisallowWallJoinAtEnd(myWall, 0);
            //                            WallUtils.DisallowWallJoinAtEnd(myWall, 1);


            //                            // Соединяю торцовую отделку
            //                            WallConnecting(firstWall, secondWall, myWall);
            //                        }
            //                    }

            //                    // Если это последняя итерация (стена - торец)
            //                    if (i == boundarySegments_list_Final.Count - 1 && (boundarySegments_list_Final[i].GetCurve().GetEndPoint(1).X != boundarySegments_list_Final[0].GetCurve().GetEndPoint(0).X
            //                        || boundarySegments_list_Final[i].GetCurve().GetEndPoint(1).Y != boundarySegments_list_Final[0].GetCurve().GetEndPoint(0).Y))
            //                    {
            //                        Line curve_3;
            //                        Curve curve_3_offset;
            //                        try
            //                        {
            //                            curve_3 = Line.CreateBound(boundarySegments_list_Final[i].GetCurve().GetEndPoint(1), // Точ в точ наооборот к прежнему случай
            //                                    boundarySegments_list_Final[0].GetCurve().GetEndPoint(0));
            //                        }
            //                        catch (Autodesk.Revit.Exceptions.ArgumentsInconsistentException ex)
            //                        {
            //                            goto Found;
            //                        }

            //                        firstWall = doc.GetElement(boundarySegments_list_Final[i].ElementId) as Wall; // Начальная точка курва
            //                        secondWall = doc.GetElement(boundarySegments_list_Final[0].ElementId) as Wall; // Конечная точка курва

            //                        // Если отделка вокруг одной стены (с разных сторон в одной помещении)
            //                        if ((firstWall != null && secondWall != null) && (firstWall.Id == secondWall.Id))
            //                        {
            //                            curve_3_offset = curve_3.CreateOffset(wallType_width * 10, new XYZ(0, 0, 1));

            //                            myWall = Wall.Create(doc,
            //                               curve_3_offset,
            //                               wallType_Id,
            //                               levelId,
            //                               5,
            //                               0,
            //                               false,
            //                               false);



            //                            // Соединяю торцовую отделку
            //                            WallUtils.AllowWallJoinAtEnd(myWall, 0);
            //                            WallUtils.AllowWallJoinAtEnd(myWall, 1);
            //                        }
            //                        else
            //                        {
            //                            curve_3_offset = curve_3.CreateOffset(wallType_width / (-2), new XYZ(0, 0, 1));

            //                            myWall = Wall.Create(doc,
            //                               curve_3_offset,
            //                               wallType_Id,
            //                               levelId,
            //                               5,
            //                               0,
            //                               false,
            //                               false);

            //                            // Отсоединяю торцовую отделку, от двух сторон (потом соединю с одной)
            //                            WallUtils.DisallowWallJoinAtEnd(myWall, 0);
            //                            WallUtils.DisallowWallJoinAtEnd(myWall, 1);


            //                            // Соединяю торцовую отделку
            //                            WallConnecting(firstWall, secondWall, myWall);
            //                        }
            //                    }
            //                Found:
            //                    int uselessNumber = 0;
            //                }
            //            }
            //            tx.Commit();
            //        }
            //    }
            //}
            #endregion
            return Result.Succeeded;
        }

        //public double mmToFeet(double mm)
        //{
        //    double mmToFeet = mm / 304.8;
        //    return mmToFeet;
        //}

        //public double feetToMm(double feet)
        //{
        //    double feetToMm = feet * 304.8;
        //    return feetToMm;
        //}

        //public void WallConnecting(Wall firstWall, Wall secondWall, Wall myWall)
        //{
        //    if (firstWall != null && secondWall != null)
        //    {
        //        if (firstWall.Width < secondWall.Width) // Если последняя стена меньше чем первая
        //        {
        //            WallUtils.AllowWallJoinAtEnd(myWall, 1); // Соединить начало торцевой отделки со стороны большой стены
        //        }
        //        else // Если первая стена меньше чем последняя
        //        {
        //            WallUtils.AllowWallJoinAtEnd(myWall, 0); // Соединить конец торцевой отделки со стороны большой стены
        //        }
        //    }
        //}

    }
}
