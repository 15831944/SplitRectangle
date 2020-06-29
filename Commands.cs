using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace SplitRectangle
{
    public class Commands
    {
        /// <summary>
        /// 画圆
        /// </summary>
        [CommandMethod("HY")]
        public void DrawCircle()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            //创建一个自定义的TypedValue列表对象，用于构建过滤器列表
            TypedValueList values = new TypedValueList();
            values.Add(typeof(Line));
            //构建过滤器列表，注意这里使用自定义类型转换
            SelectionFilter filter = new SelectionFilter(values);
            PromptSelectionResult psr = ed.GetSelection(filter);
            if (psr.Status == PromptStatus.OK)
            {
                List<Line> lines = new List<Line>();
                List<Line> horlinelist = new List<Line>();
                List<Line> verticalLinelist = new List<Line>();
                using (var trans = doc.Database.TransactionManager.StartTransaction())
                {
                    var horLines = new List<Line>();
                    var verticalLines = new List<Line>();
                    foreach (var objectId in psr.Value.GetObjectIds())
                    {
                        var line = (Line)trans.GetObject(objectId, OpenMode.ForWrite);
                        if (Math.Abs(line.Angle - 0) < 0.1 || Math.Abs(line.Angle - Math.PI) < 0.1 || Math.Abs(line.Angle - Math.PI * 2) < 0.1)
                        {
                            line.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(255, 0, 0);
                            horLines.Add(line);

                        }
                        else
                        {
                            line.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 255, 0);
                            verticalLines.Add(line);
                        }
                        horlinelist = horLines.OrderBy(l => l.StartPoint.Y).ToList();
                        verticalLinelist = verticalLines.OrderBy(l => l.StartPoint.X).ToList();
                        line.DowngradeOpen();
                    }
                    var pCrossAll = new List<Point3d>();
                    //求水平方向的每一条线
                    for (int i = 0; i < horlinelist.Count; i++)
                    {
                        foreach (var t in verticalLinelist)
                        {
                            var pCrossBottom = DBHelper.IntersectWith(t, horlinelist[i], Intersect.OnBothOperands);
                            if (pCrossBottom.Count>0)
                            {
                                pCrossAll.Add(pCrossBottom[0]);
                                //var cir = new Circle(pCrossBottom[0], Vector3d.ZAxis, 500);
                                //cir.ToSpace();
                            }
                        }
                    }
                    for (int i = 0; i < horlinelist.Count; i++)
                    {
                        var pOnlineList = pCrossAll.Where(p => IsPointOnLine(p.toPoint2d(), horlinelist[i])).OrderBy(p=>p.X).ToList();
                        var pRemains = pCrossAll.Where(p => p.Y > Math.Max(horlinelist[i].StartPoint.Y, horlinelist[i].EndPoint.Y));
                        for (int m = 0; m < pOnlineList.Count-1; m++)
                        {
                            var pBottomLeft = pOnlineList[m];
                            var pBottomRight = pOnlineList[m + 1];
                            var pTopLeft = pRemains.Where(p => Math.Abs(pBottomLeft.X - p.X) < 1).OrderBy(p => p.Y).FirstOrDefault();
                            var pTopRight = pRemains.Where(p => Math.Abs(pBottomRight.X - p.X) < 1).OrderBy(p => p.Y).FirstOrDefault();
                            var pMiddle = new Point3d((pBottomLeft.X + pTopRight.X) / 2, (pBottomLeft.Y + pTopRight.Y) / 2, 0);
                            var cir = new Circle(pMiddle, Vector3d.ZAxis, 500)
                            {
                                Color = Autodesk.AutoCAD.Colors.Color.FromRgb(255, 0, 0)
                            };
                            cir.ToSpace();
                        }

                    }
                    foreach (var item in horlinelist)
                        Console.WriteLine(item);
                    trans.Commit();
                }
            }
        }
        /// <summary>
        /// 判断2d点是否在线段上
        /// </summary>
        /// <param name="p"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public bool IsPointOnLine(Point2d p, Line line)
        {
            var flag = p.GetDistanceTo(line.StartPoint.toPoint2d()) + p.GetDistanceTo(line.EndPoint.toPoint2d()) - line.Length < 0.00001;
            return flag;
        }
    }

    public class TwoPoint
    {
        public TwoPoint(Point3d pBottem, Point3d pTop)
        {
            this.PBottom = pBottem;
            this.PTop = pTop;
        }
        public Point3d PTop { get; set; }
        public Point3d PBottom { get; set; }
    }
    public class TypedValueList : List<TypedValue>
    {
        public TypedValueList(params TypedValue[] args)
        {
            AddRange(args);//调用基类的AndRange函数填充列表
        }

        public void Add(int typecode,object value)
        {
            base.Add(new TypedValue(typecode,value));
        }
        public void Add(Type entityType)
        {
            base.Add(new TypedValue(0,RXClass.GetClass(entityType).DxfName));
        }
        public static implicit operator TypedValue[](TypedValueList src)
        {
            return src != null ? src.ToArray() : null;
        }
    }

}
