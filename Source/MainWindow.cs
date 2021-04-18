using System;
using System.Linq;
using Gtk;
using Cairo;
using System.Collections.Generic;

public partial class MainWindow : Gtk.Window
{
    public Foil selected_foil;

    public class Foil
    {
        public double[] ux;
        public double[] uy;
        public double[] lx;
        public double[] ly;

        public double margin;
        public string name;

        public Foil(double[] uxv, double[] uyv, double[] lxv, double[] lyv, string name)
        {
            this.ux = uxv;
            this.uy = uyv;
            this.lx = lxv;
            this.ly = lyv;

            this.margin = Math.Abs(uy.Max()) + Math.Abs(ly.Min());
            this.name = name;
        }
    }

    List<Foil> foils = new List<Foil>();

    public void Redraw()
    {
        _drawingarea.GdkWindow.Clear();
        double offset = 0;
        foreach (Foil f in foils)
        {
            Context cr = Gdk.CairoHelper.Create(_drawingarea.GdkWindow);

            cr.LineWidth = .003;
            if (f == selected_foil)
            {
                cr.SetSourceRGB(0.1, 0.8, 0.1);
            }
            else
            {
                cr.SetSourceRGB(0.5, 0.5, 1);
            }
            
            cr.Scale(400, 400);

            cr.Translate(0.01, f.margin + offset);
            offset += 2 * f.margin;

            int ind = 0;
            foreach (double ux in f.ux)
            {
                cr.LineTo(f.ux[ind], -f.uy[ind]);
                ind++;
            }

            ind = 0;
            foreach (double lx in f.lx)
            {
                cr.LineTo(f.lx[ind], -f.ly[ind]);
                ind++;
            }

            cr.StrokePreserve();

            cr.SelectFontFace("Courier", FontSlant.Normal, FontWeight.Normal);
            cr.SetFontSize(0.045);
            cr.TextPath(f.name);
            cr.Stroke();

            ((IDisposable)cr.GetTarget()).Dispose();
            ((IDisposable)cr).Dispose();
        }
    }

    public void ShowPointData(Foil foil)
    {
        // NACA 2412 Airfoil M = 2.0 % P = 40.0 % T = 12.0 %

        Title = "SADA - " + foil.name;

        _textview.Buffer.Text = foil.name + "\n";

        int ind = 0;
        foreach (double ux in foil.ux)
        {
            _textview.Buffer.Text += foil.ux[ind].ToString() + "    " + foil.uy[ind].ToString() + "\n";
            ind++;
        }

        ind = 0;
        foreach (double lx in foil.lx)
        {
            _textview.Buffer.Text += foil.lx[ind].ToString() + "    " + foil.ly[ind].ToString() + "\n";
            ind++;
        }
    }

    public void DeleteFoil(Foil foil)
    {
        Title = "SADA";
        foils.Remove(foil);
        Redraw();
    }

    public void InvertFoil(Foil foil)
    {
        int ind = 0;
        foreach (double y in foil.uy)
        {
            foil.uy[ind] = -y;
            ind++;
        }

        ind = 0;
        foreach (double y in foil.ly)
        {
            foil.ly[ind] = -y;
            ind++;
        }
        Redraw();
        ShowPointData(foil);
    }

    // ======================================================================

    public MainWindow() : base(Gtk.WindowType.Toplevel)
    {
        Build();
        this.Title = "SADA";
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        Application.Quit();
        a.RetVal = true;
    }

    protected void OnExit(object sender, EventArgs e)
    {
        Application.Quit();
    }

    protected void OnNew(object sender, EventArgs e)
    {
    }

    protected void OnOpen(object sender, EventArgs e)
    {
        string foil_name = "";
        Foil new_f = null;

        FileChooserDialog fcd = new FileChooserDialog("Open", this, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
        if (fcd.Run() == (int)ResponseType.Accept)
        {

            // Get File Data

            System.IO.StreamReader nfile = new System.IO.StreamReader(fcd.Filename);

            int length = 0;

            while (!nfile.EndOfStream)
            {
                string line = nfile.ReadLine();
                if (line.Contains("Airfoil"))
                {
                    foil_name = line;
                }
                else
                {
                    length++;
                }
            }

            nfile.Close();
            System.IO.StreamReader file = new System.IO.StreamReader(fcd.Filename);

            double[] ux = new double[length / 2];
            double[] uy = new double[length / 2];
            double[] lx = new double[length / 2];
            double[] ly = new double[length / 2];

            Char[] seperator = { ' ', ',' };

            int len = 0;
            while (len < length / 2)
            {
                String line = file.ReadLine();
                if (!line.Contains("Airfoil"))
                {
                    String[] splits = line.Split(seperator,StringSplitOptions.RemoveEmptyEntries);

                    if (splits.Length == 2)
                    {
                        ux[len] = Convert.ToDouble(splits[0]);
                        uy[len] = -Convert.ToDouble(splits[1]);
                    }
                    else
                    {
                        ux[len] = Convert.ToDouble(splits[1]);
                        uy[len] = -Convert.ToDouble(splits[2]);
                    }

                    len++;
                }

            }

            len = 0;

            while (len < length / 2)
            {
                String line = file.ReadLine();
                String[] splits = line.Split(seperator,StringSplitOptions.RemoveEmptyEntries);
                if (splits.Length == 2)
                {
                    lx[len] = Convert.ToDouble(splits[0]);
                    ly[len] = -Convert.ToDouble(splits[1]);
                }
                else
                {
                    lx[len] = Convert.ToDouble(splits[1]);
                    ly[len] = -Convert.ToDouble(splits[2]);
                }

                len++;
            }

            file.Close();

            Foil f = new Foil(ux, uy, lx, ly, foil_name);
            new_f = f;
            foils.Add(f);

        }

        fcd.Destroy();

        // Get Name

        Window name_dialog = new Window("Name Airfoil");

        Entry ent = new Entry("Airfoil Name");
        Button b1 = new Button("Cancel");
        Button b2 = new Button("Continue");

        void cancel_callback(object obj, EventArgs args)
        {
            new_f.name = "Imported Airfoil";
            name_dialog.Destroy();
            ShowPointData(new_f);
            selected_foil = new_f;
            Redraw();
        }
        void accept_callback(object obj, EventArgs args)
        {
            new_f.name = ent.Text;
            name_dialog.Destroy();
            ShowPointData(new_f);
            selected_foil = new_f;
            Redraw();
        }

        b1.Clicked += cancel_callback;
        b2.Clicked += accept_callback;


        HBox h1 = new HBox
            {
                b1,
                b2
            };
        VBox v1 = new VBox
            {
                ent,
                h1
            };
        name_dialog.Add(v1);
        name_dialog.ShowAll();

    }

    protected void OnAbout(object sender, EventArgs e)
    {
        AboutDialog about = new AboutDialog
        {
            Title = "About SADA",
            Version = "1.0"
        };
        string[] authors = new string[1];
        authors[0] = "Nathan Smith";
        about.Authors = authors;
        about.Comments = "SADA stands for Smith Airfoil Design Application. It was built to enable engineers to design and tweak airfoils in an easy-to-use environment.";
        //about.Copyright = "Smith Software LLC";
        about.Run();
        about.Destroy();
    }

    protected void OnCreate(object sender, EventArgs e)
    {
        double max_cam = 0;
        double max_cam_pos = 0;
        double thickness = 0;
        double precision = 0;

        double[] ux_values = new double[0];
        double[] lx_values = new double[0];
        double[] uy_values = new double[0];
        double[] ly_values = new double[0];

        Window create_dialog = new Window("Create Airfoil");
        VBox vbox = new VBox();
        HBox hbox = new HBox();
        HBox hbox2 = new HBox();
        HBox hbox3 = new HBox();
        HBox hbox4 = new HBox();
        HBox hbox5 = new HBox();
        SpinButton spin1 = new SpinButton(0, 9, 1);
        SpinButton spin2 = new SpinButton(0, 90, 10);
        SpinButton spin3 = new SpinButton(0, 50, 1);
        SpinButton spin4 = new SpinButton(100, 500, 10);
        spin1.Value = 2;
        spin2.Value = 40;
        spin3.Value = 12;
        spin4.Value = 200;

        void cancel_callback(object obj, EventArgs args)
        {
            create_dialog.Destroy();
        }
        void create_callback(object obj, EventArgs args)
        {
            max_cam = spin1.Value / 100;
            max_cam_pos = spin2.Value / 100;
            thickness = spin3.Value / 100;
            precision = spin4.Value / 2;
            create_dialog.Destroy();

            calculate_curve();
            Redraw();
        }

        Button cancel_button = new Button("Cancel");
        Button create_button = new Button("Create");
        cancel_button.Clicked += cancel_callback;
        create_button.Clicked += create_callback;

        create_dialog.Add(vbox);
        vbox.Add(hbox);
        vbox.Add(hbox2);
        vbox.Add(hbox3);
        vbox.Add(hbox4);
        vbox.Add(hbox5);
        hbox.Add(new Label("Maximum Camber:"));
        hbox.Add(spin1);
        hbox2.Add(new Label("Maximum Camber Position:"));
        hbox2.Add(spin2);
        hbox3.Add(new Label("Thickness:"));
        hbox3.Add(spin3);
        hbox4.Add(new Label("Precision:"));
        hbox4.Add(spin4);
        hbox5.Add(cancel_button);
        hbox5.Add(create_button);
        create_dialog.ShowAll();

        // Caclculate all the boring math

        void calculate_curve()
        {
            string fdi = "NACA " + (Math.Abs(max_cam) * 100).ToString()[0].ToString() + (max_cam_pos * 100).ToString()[0].ToString() + (thickness * 100).ToString()[0].ToString() + (thickness * 100).ToString()[1].ToString() + " Airfoil";

            int len = Convert.ToInt16(precision);
            double[] yc = new double[len];
            double[] dyc = new double[len];
            double[] yt = new double[len];
            double[] O = new double[len];

            ux_values = new double[len];
            lx_values = new double[len];
            uy_values = new double[len];
            ly_values = new double[len];


            double a0 = 0.2969;
            double a1 = -0.1260;
            double a2 = -0.3516;
            double a3 = 0.2843;
            double a4 = -0.1036;


            List<double> xc = new List<double>();

            //for (double n = 0; n <= 1; n += 1/precision)
            //{
            //    xc.Add(n);
            //}

            for (double n = 0; n <= Math.PI - Math.PI / precision; n += Math.PI / precision)
            {
                xc.Add((1 - Math.Cos(n)) / 2);
            }

            int index = 0;
            foreach (double x in xc)
            {
                if (0.0 <= x && x < max_cam_pos)
                {
                    //Camber
                    yc[index] = (max_cam / Math.Pow(max_cam_pos, 2)) * ((2 * max_cam_pos * x) - Math.Pow(x, 2));
                    //Gradient
                    dyc[index] = (((2 * max_cam) / Math.Pow(max_cam_pos, 2)) * (max_cam_pos - x));
                }
                else
                {
                    //Camber
                    yc[index] = (max_cam / Math.Pow((1 - max_cam_pos), 2)) * (1 - (2 * max_cam_pos) + (2 * max_cam_pos * x) - Math.Pow(x, 2));
                    //Gradient
                    dyc[index] = (((2 * max_cam) / Math.Pow((1 - max_cam_pos), 2)) * (max_cam_pos - x));
                }

                yt[index] = (thickness / 0.2) * ((a0 * Math.Pow(x, 0.5)) + (a1 * x) + (a2 * Math.Pow(x, 2)) + (a3 * Math.Pow(x, 3)) + (a4 * Math.Pow(x, 4)));

                index++;
            }

            index = 0;
            foreach (double d in dyc)
            {
                O[index] = Math.Atan(dyc[index]);

                index++;
            }

            index = 0;
            foreach (double x in xc)
            {
                //X Curve
                ux_values[index] = x - yt[index] * Math.Sin(O[index]);
                lx_values[index] = x + yt[index] * Math.Sin(O[index]);

                index++;
            }

            index = 0;
            foreach (double y in yc)
            {
                //Y Curve
                uy_values[index] = y + yt[index] * Math.Cos(O[index]);
                ly_values[index] = y - yt[index] * Math.Cos(O[index]);

                index++;
            }

            Array.Reverse(ux_values);
            Array.Reverse(uy_values);

            lx_values[0] = (lx_values[1] + lx_values[0]) / 2;
            ly_values[0] = (ly_values[1] + ly_values[0]) / 2;

            ux_values[Convert.ToInt32(precision / 2) - 1] = (ux_values[Convert.ToInt32(precision / 2) - 2] + ux_values[Convert.ToInt32(precision / 2) - 1]) / 2;
            uy_values[Convert.ToInt32(precision / 2) - 1] = (uy_values[Convert.ToInt32(precision / 2) - 2] + uy_values[Convert.ToInt32(precision / 2) - 1]) / 2;

            Foil f = new Foil(ux_values, uy_values, lx_values, ly_values, fdi);
            foils.Add(f);
            ShowPointData(f);
            selected_foil = f;


        }

    }

    protected void OnSelect(object sender, EventArgs e)
    {
        Window select_dialog = new Window("Select Airfoil");

        List<RadioButton> srbs = new List<RadioButton>();

        void cancel_callback(object obj, EventArgs args)
        {
            select_dialog.Destroy();
        }
        void select_callback(object obj, EventArgs args)
        {
            foreach (RadioButton r in srbs)
            {
                if (r.Active)
                {
                    ShowPointData(foils[srbs.IndexOf(r)]);
                    selected_foil = foils[srbs.IndexOf(r)];
                }
            }
            select_dialog.Destroy();
        }

        Button cancel_button = new Button("Cancel");
        Button select_button = new Button("Select");
        cancel_button.Clicked += cancel_callback;
        select_button.Clicked += select_callback;

        VBox vbox = new VBox();
        select_dialog.Add(vbox);
        RadioButton rbutton = new RadioButton("None");
        vbox.Add(rbutton);


        foreach (Foil f in foils)
        {
            RadioButton sb = new RadioButton(rbutton, f.name);
            srbs.Add(sb);
            vbox.Add(sb);
        }

        HBox hbox = new HBox
        {
            cancel_button,
            select_button
        };
        vbox.Add(hbox);

        select_dialog.ShowAll();
    }

    protected void OnResize(object sender, EventArgs e)
    {
        Redraw();
    }

    protected void OnClose(object sender, EventArgs e)
    {
    }

    protected void OnDelete(object sender, EventArgs e)
    {
        Window delete_dialog = new Window("Select Airfoil");

        List<RadioButton> srbs = new List<RadioButton>();

        void cancel_callback(object obj, EventArgs args)
        {
            delete_dialog.Destroy();
        }
        void select_callback(object obj, EventArgs args)
        {
            foreach (RadioButton r in srbs)
            {
                if (r.Active)
                {
                    DeleteFoil(foils[srbs.IndexOf(r)]);
                    _textview.Buffer.Text = "";
                }
            }
            delete_dialog.Destroy();
        }

        Button cancel_button = new Button("Cancel");
        Button delete_button = new Button("Delete");
        cancel_button.Clicked += cancel_callback;
        delete_button.Clicked += select_callback;

        VBox vbox = new VBox();
        delete_dialog.Add(vbox);
        RadioButton rbutton = new RadioButton("None");
        vbox.Add(rbutton);


        foreach (Foil f in foils)
        {
            RadioButton sb = new RadioButton(rbutton, f.name);
            srbs.Add(sb);
            vbox.Add(sb);
        }

        HBox hbox = new HBox
        {
            cancel_button,
            delete_button
        };
        vbox.Add(hbox);

        delete_dialog.ShowAll();
    }

    protected void OnSaveDat(object sender, EventArgs e)
    {
        FileChooserDialog fcd = new FileChooserDialog("Save As .dat", this, FileChooserAction.Save, "Cancel", ResponseType.Cancel, "Save", ResponseType.Accept);
        if (fcd.Run() == (int)ResponseType.Accept)
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(fcd.Filename);

            file.WriteLine(selected_foil.name + " Airfoil");

            int ind = 0;
            foreach (double ux in selected_foil.ux)
            {
                file.WriteLine(selected_foil.ux[ind].ToString() + "  " + (-selected_foil.uy[ind]).ToString());
                ind++;
            }

            ind = 0;
            foreach (double lx in selected_foil.lx)
            {
                file.WriteLine(selected_foil.lx[ind].ToString() + "  " + (-selected_foil.ly[ind]).ToString());
                ind++;
            }

            file.Close();
        }

        fcd.Destroy();
    }

    protected void OnSaveTxt(object sender, EventArgs e)
    {
        FileChooserDialog fcd = new FileChooserDialog("Save As .txt", this, FileChooserAction.Save, "Cancel", ResponseType.Cancel, "Save", ResponseType.Accept);
        if (fcd.Run() == (int)ResponseType.Accept)
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(fcd.Filename);

            //file.WriteLine("NACA " + selected_foil.fdi);

            int ind = 0;
            foreach (double ux in selected_foil.ux)
            {
                file.WriteLine("0.0  " + selected_foil.ux[ind].ToString() + "  " + (-selected_foil.uy[ind]).ToString());
                ind++;
            }

            ind = 0;
            foreach (double lx in selected_foil.lx)
            {
                file.WriteLine("0.0  " + selected_foil.lx[ind].ToString() + "  " + (-selected_foil.ly[ind]).ToString());
                ind++;
            }

            file.Close();
        }

        fcd.Destroy();
    }

    protected void OnInvert(object sender, EventArgs e)
    {
        Window invert_dialog = new Window("Select Airfoil");

        List<RadioButton> srbs = new List<RadioButton>();

        void cancel_callback(object obj, EventArgs args)
        {
            invert_dialog.Destroy();
        }
        void invert_callback(object obj, EventArgs args)
        {
            foreach (RadioButton r in srbs)
            {
                if (r.Active)
                {
                    InvertFoil(foils[srbs.IndexOf(r)]);
                }
            }
            invert_dialog.Destroy();
        }

        Button cancel_button = new Button("Cancel");
        Button invert_button = new Button("Invert");
        cancel_button.Clicked += cancel_callback;
        invert_button.Clicked += invert_callback;

        VBox vbox = new VBox();
        invert_dialog.Add(vbox);
        RadioButton rbutton = new RadioButton("None");
        vbox.Add(rbutton);


        foreach (Foil f in foils)
        {
            RadioButton sb = new RadioButton(rbutton, f.name);
            srbs.Add(sb);
            vbox.Add(sb);
        }

        HBox hbox = new HBox
        {
            cancel_button,
            invert_button
        };
        vbox.Add(hbox);

        invert_dialog.ShowAll();
    }

    protected void OnUpdate(object sender, EventArgs e)
    {
        string textViewData = _textview.Buffer.Text;

        Char[] nl_sep = {'\n'};
        String[] sep = { " ", "  ", "    " };

        int length = selected_foil.lx.Length * 2;
        string n_name = "";

        double[] ux = new double[length / 2];
        double[] uy = new double[length / 2];
        double[] lx = new double[length / 2];
        double[] ly = new double[length / 2];

        String[] lines = textViewData.Split(nl_sep);

        int len = -1;
        foreach (string line in lines)
        {
            if (len != -1)
            {
                if (len < length / 2)
                {
                    String[] sub_lines = line.Split(sep,StringSplitOptions.RemoveEmptyEntries);

                    if (sub_lines.Length == 2)
                    {
                        ux[len] = Convert.ToDouble(sub_lines[0]);
                        uy[len] = Convert.ToDouble(sub_lines[1]);
                    }
                    else
                    {
                        ux[len] = Convert.ToDouble(sub_lines[1]);
                        uy[len] = Convert.ToDouble(sub_lines[2]);
                    }

                    len++;
                }

                if ((length / 2) <= len && len < length)
                {
                    String[] sub_lines = line.Split(sep,StringSplitOptions.RemoveEmptyEntries);
                    if (sub_lines.Length == 2)
                    {
                        lx[len-length/2] = Convert.ToDouble(sub_lines[0]);
                        ly[len - length / 2] = Convert.ToDouble(sub_lines[1]);
                    }
                    else
                    {
                        lx[len - length / 2] = Convert.ToDouble(sub_lines[1]);
                        ly[len - length / 2] = Convert.ToDouble(sub_lines[2]);
                    }

                    len++;
                }
            }
            else
            {
                if (line.Contains("foil"))
                {
                    n_name = line;
                    selected_foil.name = n_name;
                }
                else
                {
                    n_name = line + " Airfoil";
                    selected_foil.name = n_name;
                    ShowPointData(selected_foil);
                }
                
                len++;
            }


        }
        selected_foil.ux = ux;
        selected_foil.uy = uy;
        selected_foil.lx = lx;
        selected_foil.ly = ly;
        
        Redraw();
        Title = "SADA - " + selected_foil.name;
    }
}
