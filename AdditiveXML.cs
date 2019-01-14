using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace add_stepnc
{
    class AdditiveXML
    {

        public class Layer
        {
            public int LayerNO = -1;
            public double Height = 0.0;
            public List<Polyline> Polylines = new List<Polyline>();
        }

        public class Polyline
        {
            public int PolylineID = -1;
            public string PolylineType;
            public List<Point> Points = new List<Point>();
        }

        public class Point
        {
            private double x = 0.0; public double X { get { return x; } set { x = value; } }
            private double y = 0.0; public double Y { get { return y; } set { y = value; } }
            private double z = 0.0; public double Z { get { return z; } set { z = value; } }
            public Point() { }
            public Point(double new_x, double new_y, double new_z) { x = new_x; y = new_y; z = new_z; }
        }

        public List<Layer> AdditiveLayers = new List<Layer>();

        public bool ReadXMLFile(String filename)
        {
            Queue<string> addxml = new Queue<string>(System.IO.File.ReadAllLines(filename));
            if (addxml.Count <= 0)
            {
                MessageBox.Show("Error: empty file!");
                return false;
            }

            AdditiveLayers = ParseXMLFile(addxml);

            if (AdditiveLayers.Count > 0)
                return true;

            return false;
        }

        public List<Layer> ParseXMLFile(Queue<string> lines)
        {
            string line = lines.Dequeue();
            int p_count = 0;
            int layer_count = 0;
            List<Layer> layers = new List<Layer>();

            while (!line.Contains("</Layers>"))
            {
                if(line.Contains("<Layer "))
                {
                    Layer layer = new Layer();
                    layer.LayerNO = layer_count;
                    layer.Height = Convert.ToDouble(line.TrimStart().Substring("<Layer type=\"initial\" z=\"".Length).TrimEnd('>').TrimEnd('"'));
                    line = lines.Dequeue();
                    p_count = 0;
                    while(!line.Contains("</Layer>"))
                    {
                        Polyline polyline = new Polyline();
                        polyline.PolylineID = p_count;
                        polyline.PolylineType = line.Substring(line.LastIndexOf("polylineType=\"") + 14).TrimEnd('>').TrimEnd('"');
                        line = lines.Dequeue();
                        while (!line.Contains("</Exposure>"))
                        {
                            if(line.Contains("<Point"))
                            {
                                polyline.Points.Add(ParsePoint(line));
                            }
                            line = lines.Dequeue();
                        }
                        line = lines.Dequeue();
                        layer.Polylines.Add(polyline);
                        p_count += 1;
                    }
                    if(layer.Height > 0.0)
                        layers.Add(layer);
                    layer_count += 1;
                }
                line = lines.Dequeue();
            }
            return layers;
        }

        public Point ParsePoint(string line)
        {  
            line = line.Substring(line.IndexOf('x'));
            Point point = new Point(Convert.ToDouble(line.Substring(line.IndexOf('x') + 3, line.IndexOf('"', line.IndexOf('x') + 3) - (line.IndexOf('x') + 3))),
                                    Convert.ToDouble(line.Substring(line.IndexOf('y') + 3, line.IndexOf('"', line.IndexOf('y') + 3) - (line.IndexOf('y') + 3))),
                                    Convert.ToDouble(line.Substring(line.IndexOf('z') + 3, line.IndexOf('"', line.IndexOf('z') + 3) - (line.IndexOf('z') + 3)))  
            );
            return point;
        }

        public void WriteSTEPNC(List<Layer> layers, string outname)
        {
            STEPNCLib.AptStepMaker stnc = new STEPNCLib.AptStepMaker();

            stnc.NewProjectWithCCandWP("Additive Manufacturing STEP-NC", 1, "Main Additive Workplan");//make new project

            stnc.Millimeters();

            stnc.DefineTool(1.75, 1.75 / 2, 10.0, 10.0, 1.0, 0.0, 45.0);

            foreach (Layer layer in layers)
            {
                stnc.NestWorkplan("Additive Layer-" + layer.LayerNO.ToString());

                foreach (Polyline polyline in layer.Polylines)
                {
                    stnc.LoadTool(1);//load tool
                    stnc.Workingstep("Additive WS" + "(" + polyline.PolylineType + ")" + "-" + polyline.PolylineID.ToString());
                    //stnc.Rapid();
                    bool travel = true;
                    foreach (Point point in polyline.Points)
                    {
                        if (polyline.PolylineType == "contour-open")
                        {
                            if (travel)
                            {
                                stnc.Rapid();
                                stnc.GoToXYZ(polyline.PolylineType, point.X, point.Y, point.Z);
                                stnc.Feedrate(1600.0);
                                stnc.SpindleSpeed(220);
                                travel = false;
                            }
                            else
                            {
                                stnc.GoToXYZ(polyline.PolylineType, point.X, point.Y, point.Z);
                            }
                        }
                        else if (polyline.PolylineType == "hatch")
                        {
                            if(travel)
                            {
                                stnc.Rapid();
                                stnc.GoToXYZ(polyline.PolylineType, point.X, point.Y, point.Z);
                                stnc.Feedrate(1600.0);
                                stnc.SpindleSpeed(220);
                                travel = false;
                            }
                            else
                            {
                                stnc.GoToXYZ(polyline.PolylineType, point.X, point.Y, point.Z);
                                travel = true;
                            }
                            
                        }
                    }
                }
                stnc.EndWorkplan();
            }
            stnc.SaveFastAsModules(outname);
            return;
        }
    }
}
