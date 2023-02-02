#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;

#endregion

namespace Metal_Rolling
{
    /*                                  ������
                                     17 - ������   
    
    -                                   ������
        * 1. ��������� ���������� "ADSK_����������" � "ADSK_�����" � "������������"   -> ��������� 
        * 2. ��������� ���������� �������� �� ����� ��������, ����� ��������������� ��������� �� ���������� � ��������, �.�. ���������� � ������ < Matrix.Length(1) � �������� � ������ > Matrix.Getlentgh(1) -> ���������
        * 3 MergeCells ������������ ��������� ���������� �� "���" -> ���������
        * 4. ���������� ������� �������� � ����� ��� �������� -> ���������
        * 5. �������� � ������ ������ ������ ��������� � ��������� �������� � ����� ��� ���������, �� ��� ���������� -> ���������
        * 6. ����������� �� ADSK_����� -> ��������� 
        * 7. ��������� �������� � Active.ViewSchedule ������� ����������� -> ���������
       
                                         01.12
        * 8. ���������� ������� ��� ��������� ������������� -> ���������
        * 9. ������ ���� "������������ ���������� �������� ���������� ������ �� ������" -> ���������
        * 10. ����������� ������� ��� �������� ������� -> ���������
        * 11. TableSe�tionData.SetCellStyle() - ����������: �������� ������, ������ ������, Alignment -> ���������
        * 12. TableSe�tionData.SetCellStyle() - ������ ������� ��� ������ ���������� -> 10% (������� ��������������� ������ � ������� ��� ��������� ������ � ����� �� �������)
        * 13. ���������� ������ ����� -> ���������
        * 14. ��������� ��������� ����� � ����������� ���������� -> ���������
        
        * 15. ��������� ���� ������������, �������� -> ���������
        * 16. ���� ������������ "������" ��������� ��� -> ���������
         
        * -- 17. ������� ����� ������� ����� 8-�� ������� (��� ��� �������� ����� �������)
        * 18. �������� ������ � ���������� ��������������, � ������������� ������� ������������� �� �������� (��� ��� ���� �������� ���������) + ��� ����� �� ����-�������, ���� �� ���������� ������ ���?
          *- ���� ������ ������ ��� ����� :
        
        * ������� ��� ���� ��������� ����������, ���������� � ������, ����� ����� ���� ����� �� ��������
        * � ����� ���������� ����� ���������/��������� (���� ����� ������) ����� ���������, � ������� �� � ����������, ������� ��������� � ������
        * Code Refactoring
    
     -                        ����������� ���� � ���������
         *                                28.11
           1 ������
           2 ������
         *                                29.11
           3 ������
           4 ������
           5 ������
           6 ������
         *                                01.12
           7 ������                                
         *                                02.12
           8 ������     
           9 ������
           10 ������
         *                                05.12
           11 ������
           13 ������
         *                                06.12
           14 ������
         *                                12.12
           15 ������
           16 ������
         *                                14.12
           
        */



    #region 
    [Transaction(TransactionMode.Manual)]
    public class Metal_Rolling_Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            #region Fields
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;
            #endregion







            // ������� ��� ���������� ���������� � �������
            #region
            List<FamilyInstance> fences_Instances = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StairsRailing).WhereElementIsNotElementType()
                .Cast<FamilyInstance>().OrderBy(x => x.Symbol.LookupParameter("ADSK_�����").AsValueString()).ToList();
            #endregion







            // ������� ������ ������� �������
            #region
            List<FamilyInstance> te = fences_Instances.DistinctBy(fam => fam.Name).ToList(); // ������� ���������� ����������, � ������� ���� (����� �������� �������)
            int MatrixRowCount = 0;
            int instanceCounter = 0;
            foreach (FamilyInstance item in te)
            {
                List<FamilyInstance> Test = new List<FamilyInstance>();
                foreach (var item_2 in item.GetSubComponentIds())
                {
                    Test.Add(doc.GetElement(item_2) as FamilyInstance);
                }
                MatrixRowCount += Test.DistinctBy(fam => fam.LookupParameter("ADSK_������� ��������� ���������").AsValueString()).Count();
                instanceCounter++;
            }
            #endregion







            // �������� ������ � ������������� ���������
            #region
            string[,] Matrix = new string[MatrixRowCount + 1, 8];
            string[] Arr_sech_prof_and_oboznach = new string[MatrixRowCount];
            string[] Arr_str_mat = new string[MatrixRowCount];
            #endregion







            // ������ ��������� ������, ��� ��������� ������, ����� ����� ��������� � ������ � �������� ���������� ��� "ADSK_����������" � "ADSK_�����" 
            #region 
            List<List<FamilyInstance>> ListofLists_fencesExamples = new List<List<FamilyInstance>>();
            List<List<int>> ListofLists_ADSK_Amount = new List<List<int>>();
            List<double> list_ADSK_Weight_Amount = new List<double>();
            double ADSK_Weight_Counter = 0;
            for (int i = 0; i < te.Count; i++)
            {
                ListofLists_fencesExamples.Add(new List<FamilyInstance>());
                ListofLists_ADSK_Amount.Add(new List<int>());
                List<FamilyInstance> famTest = new List<FamilyInstance>();
                foreach (var item_2 in te[i].GetSubComponentIds())
                {
                    FamilyInstance fam = doc.GetElement(item_2) as FamilyInstance;
                    famTest.Add(fam); // ��� ������������ �������� ����� ����������
                    if (fam.Name != "RSm_������������_r22_v0.01")
                    {
                        ADSK_Weight_Counter += fam.LookupParameter("ADSK_�����").AsDouble();
                    }
                }
                // �������� ��������� ������  
                ListofLists_fencesExamples[i] = famTest.DistinctBy(elem => elem.LookupParameter("ADSK_������� ��������� ���������").AsValueString())
                    .OrderBy/*(x => x.LookupParameter("ADSK_�����").AsValueString()).ThenBy*/(x => x.LookupParameter("ADSK_������� ��������� ���������").AsValueString()).ToList(); // DistinctBy and OrderBy
                // ������� ���������� ������ ������������ �������
                for (int w = 0; w < famTest.Count; w++)
                {
                    foreach (Parameter item in famTest[w].Parameters)
                    {
                        if (item.Definition.Name == "ADSK_����������" && item.AsDouble() == 0)
                        {
                            famTest.RemoveAt(w);
                            break;
                        }
                    }
                }
                var t_str = famTest.Select(fam => fam.LookupParameter("ADSK_������� ��������� ���������").AsValueString()).ToList();
                t_str.Sort();
                ListofLists_ADSK_Amount[i] = t_str.GroupBy(x => x).Where(g => g.Count() > 0).Select(x => x.Count()).ToList();
                // ������� ����� ���� ������������ ���������
                list_ADSK_Weight_Amount.Add(ADSK_Weight_Counter);
                ADSK_Weight_Counter = 0;
            }
            #endregion







            // ������� ��� � ������, ����� ����� ��������� � ��������� (��� �������)
            #region         

            List<Material> Materials = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Materials).Cast<Material>().ToList();
            int listCounter = 0;
            string MirrorFor_Matrix_0_2 = "";
            for (int p = 0; p < ListofLists_fencesExamples.Count; p++)
            {
                #region
                if (p == 0) // ���� ��� ������ ����������
                {
                    for (int i = 0; i < ListofLists_fencesExamples[p].Count; i++) // i - ��� ������ ������� 
                    {
                        if (ListofLists_fencesExamples[p][i].Name == "RSm_������������_r22_v0.01") // ���� �������� ����
                        {
                            Matrix[i, 0] = te[p].Symbol.LookupParameter("ADSK_�����").AsValueString();

                            Matrix[i, 1] = ListofLists_fencesExamples[p][i].LookupParameter("ADSK_������� ��������� ���������").AsValueString();

                            Matrix[i, 2] = ListofLists_fencesExamples[p][i].LookupParameter("ADSK_������������").AsValueString();
                            Matrix[i, 3] = ListofLists_fencesExamples[p][i].LookupParameter("ADSK_������������").AsValueString();
                            Matrix[i, 4] = ListofLists_fencesExamples[p][i].LookupParameter("ADSK_������������").AsValueString();

                            Matrix[i, 5] = ListofLists_ADSK_Amount[p][i].ToString();

                            Matrix[i, 6] = "-";

                            Matrix[i, 7] = Math.Round(list_ADSK_Weight_Amount[p], 2).ToString();
                        }
                        else if (ListofLists_fencesExamples[p][i].Name == "RS_����������������_����34028-2016_r22_v0.01") // ���� ������
                        {
                            Matrix[i, 0] = te[p].Symbol.LookupParameter("ADSK_�����").AsValueString();

                            Matrix[i, 1] = ListofLists_fencesExamples[p][i].LookupParameter("ADSK_������� ��������� ���������").AsValueString();

                            // ������������ ���������� ������
                            string str_naim = ListofLists_fencesExamples[p][i].LookupParameter("ADSK_������������ �������").AsValueString();
                            string str_Sost_postavk = ListofLists_fencesExamples[p][i].LookupParameter("��������� ��������").AsValueString();
                            string str_D_test = ListofLists_fencesExamples[p][i].LookupParameter("ADSK_������� ��������").AsValueString();
                            string str_Diametr = str_D_test.Substring(0, str_D_test.IndexOf(' '));
                            string str_Dlina = ListofLists_fencesExamples[p][i].LookupParameter("ADSK_������_�����").AsValueString();
                            string str_Klass_armatura = ListofLists_fencesExamples[p][i].LookupParameter("����� ��������").AsValueString();
                            string str_Oboznach = ListofLists_fencesExamples[p][i].Symbol.LookupParameter("ADSK_�����������").AsValueString();
                            // ������������ ���������� �����

                            Matrix[i, 2] = str_naim + "  " + str_Sost_postavk + "-" + str_Diametr + "X" + str_Dlina + "-" + str_Klass_armatura + " " + str_Oboznach;
                            Matrix[i, 3] = str_naim + "  " + str_Sost_postavk + "-" + str_Diametr + "X" + str_Dlina + "-" + str_Klass_armatura + " " + str_Oboznach;
                            Matrix[i, 4] = str_naim + "  " + str_Sost_postavk + "-" + str_Diametr + "X" + str_Dlina + "-" + str_Klass_armatura + " " + str_Oboznach;

                            Matrix[i, 5] = ListofLists_ADSK_Amount[p][i].ToString();

                            Matrix[i, 6] = Math.Round(ListofLists_fencesExamples[p][i].LookupParameter("ADSK_�����").AsDouble(), 2).ToString();

                            Matrix[i, 7] = Math.Round(list_ADSK_Weight_Amount[p], 2).ToString();
                        }
                        else // �����
                        {
                            Matrix[i, 0] = te[p].Symbol.LookupParameter("ADSK_�����").AsValueString();

                            Matrix[i, 1] = ListofLists_fencesExamples[p][i].LookupParameter("ADSK_������� ��������� ���������").AsValueString();

                            // ������������ ���������� ������
                            string str_naim_kratk = "";
                            string str_sech_prof = "";
                            string str_oboznach = "";
                            string str_mat = "";
                            string str_dlina = "";

                            str_naim_kratk = ListofLists_fencesExamples[p][i].LookupParameter("ADSK_������������ �������").AsValueString();
                            if (ListofLists_fencesExamples[p][i].Name == "RSh_�����_����32931-2015_r22_v0.01")
                            {
                                str_sech_prof = ListofLists_fencesExamples[p][i].LookupParameter("������� �������").AsValueString();
                            }
                            else
                            {
                                str_sech_prof = ListofLists_fencesExamples[p][i].LookupParameter("������� ������").AsValueString() + "-" +
                                                ListofLists_fencesExamples[p][i].LookupParameter("�������� ��������").AsValueString() + "-" +
                                                ListofLists_fencesExamples[p][i].LookupParameter("����� �������������").AsValueString();
                            }
                            str_oboznach = ListofLists_fencesExamples[p][i].Symbol.LookupParameter("ADSK_�����������").AsValueString();
                            foreach (var item in Materials)
                            {
                                if (ListofLists_fencesExamples[p][i].LookupParameter("ADSK_��������").AsValueString() == item.Name)
                                {
                                    str_mat = item.LookupParameter("ADSK_�������� ������������").AsValueString() + "  " + item.LookupParameter("ADSK_�������� �����������").AsValueString();
                                }
                            }
                            str_dlina = "L=" + ListofLists_fencesExamples[p][i].LookupParameter("ADSK_������_�����").AsValueString() + "��";
                            string sech_prof_and_oboznach = str_sech_prof + "  " + str_oboznach;
                            MirrorFor_Matrix_0_2 = str_naim_kratk + sech_prof_and_oboznach + str_dlina;
                            // ������������ ���������� �����

                            Matrix[i, 2] = str_naim_kratk;
                            Matrix[i, 3] = sech_prof_and_oboznach + "\n" + str_mat;
                            Arr_sech_prof_and_oboznach[i] = sech_prof_and_oboznach;
                            Arr_str_mat[i] = str_mat;

                            Matrix[i, 4] = str_dlina;
                            // 
                            Matrix[i, 5] = ListofLists_ADSK_Amount[p][i].ToString();

                            Matrix[i, 6] = Math.Round(ListofLists_fencesExamples[p][i].LookupParameter("ADSK_�����").AsDouble(), 2).ToString();

                            Matrix[i, 7] = Math.Round(list_ADSK_Weight_Amount[p], 2).ToString();
                        }
                        // � �������, ���� ���������� ����� ������������ ��������, �� ������ �������� ����� ������� � ����� � ����
                    }
                    listCounter += ListofLists_fencesExamples[p].Count;
                }

                #endregion
                #region
                else // ���� ��� �� ������ ���������� (� ����� ��� ��� � ���, ��� � ������ �������)
                {
                    int someNumber = 0;
                    for (int i = listCounter; i < listCounter + ListofLists_fencesExamples[p].Count; i++) // i - ��� ������ ������� 
                    {
                        if (ListofLists_fencesExamples[p][someNumber].Name == "RSm_������������_r22_v0.01") // ���� �������� ����
                        {
                            Matrix[i, 0] = te[p].Symbol.LookupParameter("ADSK_�����").AsValueString();

                            Matrix[i, 1] = ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_������� ��������� ���������").AsValueString();

                            Matrix[i, 2] = ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_������������").AsValueString();
                            Matrix[i, 3] = ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_������������").AsValueString();
                            Matrix[i, 4] = ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_������������").AsValueString();

                            Matrix[i, 5] = ListofLists_ADSK_Amount[p][someNumber].ToString();

                            Matrix[i, 6] = "-";

                            Matrix[i, 7] = Math.Round(list_ADSK_Weight_Amount[p], 2).ToString();

                            someNumber++;
                        }
                        else if (ListofLists_fencesExamples[p][someNumber].Name == "RS_����������������_����34028-2016_r22_v0.01") // ���� ������
                        {
                            Matrix[i, 0] = te[p].Symbol.LookupParameter("ADSK_�����").AsValueString();

                            Matrix[i, 1] = ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_������� ��������� ���������").AsValueString();

                            // ������������ ���������� ������
                            string str_naim = ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_������������ �������").AsValueString();
                            string str_Sost_postavk = ListofLists_fencesExamples[p][someNumber].LookupParameter("��������� ��������").AsValueString();
                            string str_D_test = ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_������� ��������").AsValueString();
                            string str_Diametr = str_D_test.Substring(0, str_D_test.IndexOf(' '));
                            string str_Dlina = ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_������_�����").AsValueString();
                            string str_Klass_armatura = ListofLists_fencesExamples[p][someNumber].LookupParameter("����� ��������").AsValueString();
                            string str_Oboznach = ListofLists_fencesExamples[p][someNumber].Symbol.LookupParameter("ADSK_�����������").AsValueString();
                            // ������������ ���������� �����
                            Matrix[i, 2] = str_naim + "  " + str_Sost_postavk + "-" + str_Diametr + "X" + str_Dlina + "-" + str_Klass_armatura + " " + str_Oboznach;
                            Matrix[i, 3] = str_naim + "  " + str_Sost_postavk + "-" + str_Diametr + "X" + str_Dlina + "-" + str_Klass_armatura + " " + str_Oboznach;
                            Matrix[i, 4] = str_naim + "  " + str_Sost_postavk + "-" + str_Diametr + "X" + str_Dlina + "-" + str_Klass_armatura + " " + str_Oboznach;

                            Matrix[i, 5] = ListofLists_ADSK_Amount[p][someNumber].ToString();

                            Matrix[i, 6] = Math.Round(ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_�����").AsDouble(), 2).ToString();

                            Matrix[i, 7] = Math.Round(list_ADSK_Weight_Amount[p], 2).ToString();

                            someNumber++;
                        }
                        else // �����
                        {
                            Matrix[i, 0] = te[p].Symbol.LookupParameter("ADSK_�����").AsValueString();

                            Matrix[i, 1] = ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_������� ��������� ���������").AsValueString();

                            // ������������ ���������� ������
                            string str_naim_kratk = "";
                            string str_sech_prof = "";
                            string str_oboznach = "";
                            string str_mat = "";
                            string str_dlina = "";

                            str_naim_kratk = ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_������������ �������").AsValueString();
                            if (ListofLists_fencesExamples[p][someNumber].Name == "RSh_�����_����32931-2015_r22_v0.01")
                            {
                                str_sech_prof = ListofLists_fencesExamples[p][someNumber].LookupParameter("������� �������").AsValueString();
                            }
                            else
                            {
                                str_sech_prof = ListofLists_fencesExamples[p][someNumber].LookupParameter("������� ������").AsValueString() + "-" +
                                                ListofLists_fencesExamples[p][someNumber].LookupParameter("�������� ��������").AsValueString() + "-" +
                                                ListofLists_fencesExamples[p][someNumber].LookupParameter("����� �������������").AsValueString();
                            }
                            str_oboznach = ListofLists_fencesExamples[p][someNumber].Symbol.LookupParameter("ADSK_�����������").AsValueString();
                            foreach (var item in Materials)
                            {
                                if (ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_��������").AsValueString() == item.Name)
                                {
                                    str_mat = item.LookupParameter("ADSK_�������� ������������").AsValueString() + " " + item.LookupParameter("ADSK_�������� �����������").AsValueString();
                                }
                            }
                            str_dlina = "L=" + ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_������_�����").AsValueString() + "��";

                            string sech_prof_and_oboznach = str_sech_prof + str_oboznach;
                            string whyteSpace = "";
                            string Space_naim_krat = whyteSpace.PadRight(str_naim_kratk.Length);
                            //// ������������ ���������� �����
                            Matrix[i, 2] = str_naim_kratk;
                            Matrix[i, 3] = sech_prof_and_oboznach + "\n" + str_mat;
                            Arr_sech_prof_and_oboznach[i] = sech_prof_and_oboznach;
                            Arr_str_mat[i] = str_mat;

                            Matrix[i, 4] = str_dlina;
                            //

                            Matrix[i, 5] = ListofLists_ADSK_Amount[p][someNumber].ToString();

                            Matrix[i, 6] = Math.Round(ListofLists_fencesExamples[p][someNumber].LookupParameter("ADSK_�����").AsDouble(), 2).ToString();

                            Matrix[i, 7] = Math.Round(list_ADSK_Weight_Amount[p], 2).ToString();

                            someNumber++;
                        }
                        // � �������, ���� ���������� ����� ������������ ��������, �� ������ �������� ����� ������� � ����� � ����
                    }
                    listCounter += ListofLists_fencesExamples[p].Count;
                }
                #endregion 
            }
            Matrix = Array2DRowDoubler(Matrix);
            Arr_sech_prof_and_oboznach = ArrayRowDoubler(Arr_sech_prof_and_oboznach);
            Arr_str_mat = ArrayRowDoubler(Arr_str_mat);
            #endregion







            // ���� �������� � ���������            
            #region
            ViewSchedule vs = doc.ActiveView as ViewSchedule; // ������� ��������� ��� ��� ��� ������������/���������
            if (null == vs)
            {
                TaskDialog.Show("t", "������ ��� �� �������� �������������"); // ������ ���� ������������� ��� �� ������������
            }
            else
            {
                using (Transaction tx = new Transaction(doc))
                {
                    // 1 Letter Revit/VS = 0.01736 mm
                    // 1 mm = 1/304.8 feet
                    // 1 letter.width = letter * 0.01736 / 304.8
                    //double simple_letter_To_Revit_Letter = (3 * 1.736 / 304.8) / 2;


                    // ������� ����������
                    tx.Start("Transaction Name");
                    TableData td = vs.GetTableData(); // get viewschedule table data
                    TableSectionData tsd = td.GetSectionData(SectionType.Header); // get header section data
                    // ���������� ������� ��� ��������� �������������                
                    if (tsd.NumberOfRows > 1)
                    {
                        // ������� ������
                        for (int r = tsd.NumberOfRows - 1; r > 0; r--)
                        {
                            tsd.RemoveRow(r);
                        }
                    }
                    // MergeCells ���������� ������ - ��������� ������ � ������ ��������
                    TableMergedCell mergecell_Marka = new TableMergedCell();
                    mergecell_Marka.Left = 0;
                    mergecell_Marka.Right = 0;
                    mergecell_Marka.Top = 1;
                    mergecell_Marka.Bottom = 0;

                    TableMergedCell mergecell_Massa = new TableMergedCell();
                    mergecell_Massa.Left = 7;
                    mergecell_Massa.Right = 7;
                    mergecell_Massa.Top = 1;
                    mergecell_Massa.Bottom = 0;
                    // MergeCells ���������� ���������


                    // �������� �����
                    for (int i = 0; i < Matrix.GetLength(0); i++)
                    {
                        if (i == 0) // �������� ���� ���� ��� ������ �� ������ �� � ������ �����
                        {
                            tsd.InsertRow(1);
                            tsd.RemoveRow(0);
                            // ����������� ����� �������� (������)
                            if (tsd.NumberOfColumns < Matrix.GetLength(1))
                            {
                                for (int m = tsd.NumberOfColumns; m < Matrix.GetLength(1); m++)
                                {
                                    tsd.InsertColumn(m);
                                }
                            }
                            else
                            {
                                for (int m = Matrix.GetLength(1); m < tsd.NumberOfColumns; m++)
                                {
                                    tsd.RemoveColumn(m);
                                }
                            }
                            // ����������� ����� �������� (�����) 

                            // ������������ ������ �������� ������� - ������                        
                            for (int j = 0; j < Matrix.GetLength(1); j++)
                            {
                                if (j == 0 || j == 6 || j == 7)
                                {
                                    tsd.SetColumnWidth(j, 15 / 304.8 /*(MirrorFor_Matrix_0_2.Length + 15) * simple_letter_To_Revit_Letter*/);
                                }
                                else if (j == 1 || j == 5)
                                {
                                    tsd.SetColumnWidth(j, 10 / 304.8 /*(Matrix[0, j].Length + 5) * simple_letter_To_Revit_Letter*/);
                                }
                                else if (j == 2 || j == 4)
                                {
                                    tsd.SetColumnWidth(j, 14 / 304.8);
                                }
                                else if (j == 3)
                                {
                                    tsd.SetColumnWidth(j, 32 / 304.8);
                                }
                            }
                            // ������������ ������ �������� ������� - ��������� (������������ ����� ������� ����� ������)

                            // ������������ �������� �������� �������
                            tsd.SetCellText(i, 0, "����� �������");
                            tsd.SetCellText(i, 1, "���. ���.");
                            tsd.SetCellText(i, 2, "������������");
                            tsd.SetCellText(i, 3, "������������");
                            tsd.SetCellText(i, 4, "������������");
                            tsd.SetCellText(i, 5, "���.");
                            tsd.SetCellText(i, 6, "����� 1 ���., ��");
                            tsd.SetCellText(i, 7, "����� �������, ��");

                            // ������������ ������ ����� ������� - header
                            tsd.SetRowHeight(i, 15 / 304.8);
                        }
                        else // ���� ��� �� ������ ������
                        {
                            tsd.InsertRow(i); // �������� ����� ������

                            for (int j = 0; j < Matrix.GetLength(1); j++)
                            {
                                if (Matrix[i - 1, j] != null)
                                {
                                    tsd.SetCellText(i, j, Matrix[i - 1, j]);  // ���� �������� ��� ��� ���� �� ������ ������ ������ ������� ������ ��� ������, � � ������� ����� ��������� ��� ����� �� ���� � ��� ������
                                }
                            }

                            // MergeCells ������ ������
                            if (tsd.GetCellText(i, 0) != tsd.GetCellText(mergecell_Marka.Top, 0))
                            {
                                mergecell_Marka.Bottom = i - 1;
                                mergecell_Massa.Bottom = i - 1;
                                tsd.MergeCells(mergecell_Marka);
                                tsd.MergeCells(mergecell_Massa);
                                mergecell_Marka.Top = i;
                                mergecell_Massa.Top = i;
                            }
                            if (i == tsd.NumberOfRows - 1)
                            {
                                mergecell_Marka.Bottom = i;
                                mergecell_Massa.Bottom = i;
                                tsd.MergeCells(mergecell_Marka);
                                tsd.MergeCells(mergecell_Massa);
                            }
                            // MergeCells ������ ���������

                            // ������������ ������ ����� ������� - body
                            tsd.SetRowHeight(i, 8 / 304.8);
                        }
                        // ������������ ������ ����� �������

                    }

                    // ������ �������� ������ ������
                    string deleteCheck = "";
                    for (int i = 0; i < tsd.NumberOfRows; i++)
                    {
                        for (int j = 0; j < tsd.NumberOfColumns; j++)
                        {
                            deleteCheck += tsd.GetCellText(i, j).Trim();
                        }
                        if (deleteCheck.Trim() == "")
                        {
                            tsd.RemoveRow(i);
                        }
                        deleteCheck = "";
                    }
                    ////

                    // �������������� ������

                    // Get the current style of the title cell
                    //TableCellStyle tableCellStyle = tsd.GetTableCellStyle(1, 1);
                    TableCellStyle tableCellStyle = new TableCellStyle();

                    // Get the override options of the style
                    //TableCellStyleOverrideOptions overrideOptions = tableCellStyle.GetCellStyleOverrideOptions();
                    TableCellStyleOverrideOptions overrideOptions = new TableCellStyleOverrideOptions();

                    // Font Size
                    overrideOptions.FontSize = true;

                    // Font Name
                    overrideOptions.Font = true;

                    // Horizontal Alignment
                    overrideOptions.HorizontalAlignment = true;

                    // Set the overrides options on the style
                    tableCellStyle.SetCellStyleOverrideOptions(overrideOptions);

                    // Change Font Name
                    tableCellStyle.FontName = "GOST Common";

                    // ������������ ������ ������ ��� ���� �����, ��������� 3.5 ��, ���������� 2.5 ��
                    for (int i = 0; i < tsd.NumberOfRows; i++)
                    {
                        for (int j = 0; j < tsd.NumberOfColumns; j++)
                        {
                            tableCellStyle.TextSize = 2.5 * 3.77938;
                            tsd.SetCellStyle(i, j, tableCellStyle);
                        }
                    }

                    // Horizontal Alignment                  
                    tableCellStyle.FontHorizontalAlignment = HorizontalAlignmentStyle.Left;
                    for (int i = 1; i < tsd.NumberOfRows; i++)
                    {
                        tsd.SetCellStyle(i, 2, tableCellStyle); // ��� ��������� ��� ���� ������������
                    }

                    //

                    // 15-� � �������� ������ ���������� ������

                    // ��������� �������� ��� ������� ����� �� ������������ ���������� Sech_prof �  mat division 
                    for (int i = 1; i < tsd.NumberOfRows - 1; i++)
                    {
                        if (Arr_sech_prof_and_oboznach[i - 1] != null && Arr_str_mat[i - 1] != null)
                        {
                            if (i % 2 != 0)
                            {
                                tsd.SetCellText(i, 3, Arr_sech_prof_and_oboznach[i - 1]);
                            }
                            else
                            {
                                tsd.SetCellText(i, 3, Arr_str_mat[i - 1]);
                            }
                        }
                    }
                    ////



                    //// ������ ������� ��� ������ ���������� 
                    //GraphicsStyle GraphicsStyle_lineStyle = new FilteredElementCollector(doc).OfClass(typeof(GraphicsStyle)).Cast<GraphicsStyle>().FirstOrDefault(e => e.Name.Equals("<���������� �����>"));
                    //overrideOptions.BorderLineStyle = true;
                    //overrideOptions.BorderBottomLineStyle = true;
                    //overrideOptions.BorderTopLineStyle = true;
                    //overrideOptions.BorderLeftLineStyle = true;
                    //overrideOptions.BorderRightLineStyle = true;

                    //tableCellStyle.SetCellStyleOverrideOptions(overrideOptions);

                    //tableCellStyle.BorderTopLineStyle = GraphicsStyle_lineStyle.GraphicsStyleCategory.Id;
                    //tableCellStyle.BorderBottomLineStyle = GraphicsStyle_lineStyle.GraphicsStyleCategory.Id;
                    //tableCellStyle.BorderRightLineStyle = GraphicsStyle_lineStyle.GraphicsStyleCategory.Id;
                    //tableCellStyle.BorderLeftLineStyle = GraphicsStyle_lineStyle.GraphicsStyleCategory.Id;

                    ////tableCellStyle.BorderTopLineStyle = GraphicsStyle_lineStyle.Id;
                    ////tableCellStyle.BorderBottomLineStyle = GraphicsStyle_lineStyle.Id;
                    ////tableCellStyle.BorderRightLineStyle = GraphicsStyle_lineStyle.Id;
                    ////tableCellStyle.BorderLeftLineStyle = GraphicsStyle_lineStyle.Id;

                    //tsd.SetCellStyle(1, 2, tableCellStyle);
                    ////tsd.SetCellStyle(2, 3, tableCellStyle);
                    ////tsd.SetCellStyle(1, 4, tableCellStyle);
                    ////tsd.SetCellStyle(2, 4, tableCellStyle);

                    ////

                    
                    //// �������� ������ ��� ���� ������������
                    overrideOptions.BorderLineStyle = true;
                    overrideOptions.BorderBottomLineStyle = false;
                    overrideOptions.BorderTopLineStyle = false;
                    overrideOptions.BorderLeftLineStyle = true;
                    overrideOptions.BorderRightLineStyle = false;

                    tableCellStyle.SetCellStyleOverrideOptions(overrideOptions);

                    for (int i = 1; i < tsd.NumberOfRows; i++)
                    {
                        for (int j = 3; j < 5; j++) // ��� ��� BorderLineStyle �������� ������ � Left ������������ ��������� �������
                        {
                            tsd.SetCellStyle(i, j, tableCellStyle);
                        }
                        tsd.SetRowHeight(i, 4 / 304.8); // SetRowHeight for Doubled rows
                    }
                    
                    //// Merging doubled rows, and column ������������ ������� (� ���� ��� 3 �������)
                    TableMergedCell mergecell_test = new TableMergedCell();

                    // Merge ������������ �������������, ��� ��� �������
                    mergecell_test.Left = 2;
                    mergecell_test.Right = 4;
                    mergecell_test.Top = 0;
                    mergecell_test.Bottom = 0;
                    tsd.MergeCells(mergecell_test);

                    // Merge ��������� ����� 4-� �������
                    for (int i = 1; i < tsd.NumberOfRows; i += 2)
                    {
                        for (int j = 1; j < 7; j++)
                        {
                            if (j != 3 || (j == 3 && (tsd.GetCellText(i, j).Contains("��������") || tsd.GetCellText(i, j).Contains("������"))))
                            {
                                mergecell_test.Left = j;
                                mergecell_test.Right = j;
                                mergecell_test.Top = i;
                                mergecell_test.Bottom = i + 1;
                                tsd.MergeCells(mergecell_test);
                            }
                        }
                        for (int j = 1; j < 7; j++)
                        {
                            if (tsd.GetCellText(i, j).Contains("��������") || tsd.GetCellText(i, j).Contains("������"))
                            {
                                for (int c = 2; c < 5; c++)
                                {
                                    mergecell_test.Left = 2;
                                    mergecell_test.Right = 4;
                                    mergecell_test.Top = i;
                                    mergecell_test.Bottom = i + 1;
                                    tsd.MergeCells(mergecell_test);
                                }
                                break;
                            }
                        }
                    }

                    // HorizontalAlignmentStyle.Center for "L="
                    for (int i = 1; i < tsd.NumberOfRows; i++)
                    {
                        tableCellStyle.FontHorizontalAlignment = HorizontalAlignmentStyle.Center;
                        tsd.SetCellStyle(i, 4, tableCellStyle); // ��� ��������� ��� ���� ������������
                    }
                    
                    // ���������� ����������
                    tx.Commit();
                }
            }
            #endregion
            return Result.Succeeded;
        }

        static string[,] Array2DRowDoubler(string[,] Array2D)
        {
            string[,] RowDoubled_2DArray = new string[Array2D.GetLength(0) * 2, Array2D.GetLength(1)];
            int a = 0;
            for (int i = 0; i < Array2D.GetLength(0); i++)
            {
                for (int c = 0; c < 2; c++)
                {
                    for (int j = 0; j < Array2D.GetLength(1); j++)
                    {
                        RowDoubled_2DArray[a, j] = Array2D[i, j];
                    }
                    a++;
                }
            }
            return RowDoubled_2DArray;
        } // Method for Doubling Rows of 2D Array
        static string[] ArrayRowDoubler(string[] Array)
        {
            string[] RowDoubled_Array = new string[Array.Length * 2];
            int a = 0;
            for (int i = 0; i < Array.Length; i++)
            {
                for (int c = 0; c < 2; c++)
                {
                    RowDoubled_Array[a] = Array[i];
                    a++;
                }
            }
            return RowDoubled_Array;
        } // Method for Doubling Rows of Array
    }
    #endregion
}
