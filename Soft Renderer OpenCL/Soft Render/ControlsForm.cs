using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Soft_Renderer
{
    public partial class ControlsForm : Form
    {

        public static long fulltime, calctime, shadowtime;

        

        string texture = "RF-15_PeakEagle_P01.png";
        string obj = "RF-15_PeakEagle.obj";
        Timer t;
        public RenderingServer server;

        bool auto = false;

        FormMain main;


        public ControlsForm(FormMain main)
        {
            Location = new Point((int)(Screen.PrimaryScreen.WorkingArea.Width - Width*1.7), 0);
            InitializeComponent();
            this.main = main;

            numericUpDown1step.Increment = new decimal(0.1);
            numericUpDown1brightness.Increment = new decimal(0.1);

            t = new Timer();
            t.Interval = 10;
            t.Tick += T_Tick;


            main.ShowTime += Main_ShowTime;

        }

        private void Main_ShowTime(object sender, EventArgs e)
        {
            label8time.Text = ("Full: " + TimeSpan.FromTicks(fulltime).ToString() +
                        "\nCalc: " + TimeSpan.FromTicks(calctime).ToString() +
                        "\nShadow: " + TimeSpan.FromTicks(shadowtime).ToString());
        }

        private void T_Tick(object sender, EventArgs e)
        {
            if (main.inProcess) return;
            if (main.r == null) return;
            main.r.AngleY += 0.05;
            main.Upd();          
        }

        private void button1zoomp_Click(object sender, EventArgs e)
        {
            if (main.r == null) return;
            main.r.Zoom *= 2;
            if (auto) main.Upd();
        }

        private void button2zoomm_Click(object sender, EventArgs e)
        {
            if (main.r == null) return;
            main.r.Zoom /= 2;
            if (auto) main.Upd();
        }

        private void button11xp_Click(object sender, EventArgs e)
        {
            if (main.r == null) return;
            main.r.PosX -= 10;
            if (auto) main.Upd();
        }

        private void button12xm_Click(object sender, EventArgs e)
        {
            if (main.r == null) return;
            main.r.PosX += 10;
            if (auto) main.Upd();
        }

        private void button9yp_Click(object sender, EventArgs e)
        {
            if (main.r == null) return;
            main.r.PosY += 10;
            if (auto) main.Upd();
        }

        private void button10ym_Click(object sender, EventArgs e)
        {
            if (main.r == null) return;
            main.r.PosY -= 10;
            if (auto) main.Upd();
        }

        private void button6aym_Click(object sender, EventArgs e)
        {
            if (main.r == null) return;
            main.r.AngleY += 0.2;
            if (auto) main.Upd();
        }

        private void button5ayp_Click(object sender, EventArgs e)
        {
            if (main.r == null) return;
            main.r.AngleY -= 0.2;
            if (auto) main.Upd();
        }

        private void button3axp_Click(object sender, EventArgs e)
        {
            if (main.r == null) return;
            main.r.AngleX -= 0.2;
            if (auto) main.Upd();
        }

        private void button4axm_Click(object sender, EventArgs e)
        {
            if (main.r == null) return;
            main.r.AngleX += 0.2;
            if (auto) main.Upd();
        }

        private void button8azm_Click(object sender, EventArgs e)
        {
            if (main.r == null) return;
            main.r.AngleZ -= 0.2;
            if (auto) main.Upd();
        }

        private void button7azp_Click(object sender, EventArgs e)
        {
            if (main.r == null) return;
            main.r.AngleZ += 0.2;
            if (auto) main.Upd();
        }

        private void button1lzp_Click(object sender, EventArgs e)
        {
            if (main.r == null) return;
            main.r.LightZ = -2000;
            if (auto) main.Upd();
        }

        private void button1lzm_Click(object sender, EventArgs e)
        {
            if (main.r == null) return;
            main.r.LightZ = 2000;
            if (auto) main.Upd();
        }

        private void button1lxm_Click(object sender, EventArgs e)
        {
            if (main.r == null) return;
            main.r.LightX = 2000;
            if (auto) main.Upd();
        }

        private void button1lxp_Click(object sender, EventArgs e)
        {
            if (main.r == null) return;
            main.r.LightX = -2000;
            if (auto) main.Upd();
        }

        private void button1obj_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.ShowDialog();
            obj = d.FileName;
        }

        private void button3run_Click(object sender, EventArgs e)
        {
            try
            {
                main.r = new Renderer((int)numericUpDown1width.Value, (int)numericUpDown1heigth.Value, obj, texture);

                checkBox1texture.Checked = main.r.useTexture;
                checkBox1web.Checked = main.r.polygonWeb;
                checkBox1nolight.Checked = main.r.noLight;
                checkBox1norm.Checked = main.r.midNormales;
                checkBox1shadow.Checked = main.r.shadow;
                checkBox1optimization.Checked = main.r.optimization;
                checkBox1indent.Checked = main.r.indent == 0 ? false : true;
                radioButton1phong.Checked = !main.r.ambient_occlusion;
                radioButton2ambient.Checked = main.r.ambient_occlusion;
                numericUpDown1numlights.Value = new decimal(main.r.lightsNum);
                numericUpDown1step.Value = new decimal(main.r.quality);
                numericUpDown1brightness.Value = new decimal(main.r.brightness);
                numericUpDown1lightsforserver.Value = new decimal(main.r.lightsForServer);

                checkBox1background.Checked = false;

            }
            catch
            {
                MessageBox.Show("Loading error");
                return;
            }
        }

        private void button2png_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.ShowDialog();
            texture = d.FileName;
        }

        private void button1render_Click(object sender, EventArgs e)
        {
            if (main.r == null) return;
            main.Upd();
        }

        private void checkBox1timer_CheckedChanged(object sender, EventArgs e)
        {
            if (main.r == null) return;
            if (checkBox1timer.Checked) t.Start();
            else t.Stop();
        }


        private void checkBox1auto_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1auto.Checked) auto = true;
            else auto = false;
        }

        private void button1save_Click(object sender, EventArgs e)
        {
            main.Save();
        }

        private void ControlsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            main.Close();
        }

        private void checkBox1texture_CheckedChanged(object sender, EventArgs e)
        {
            if (main.r == null) return;
            if (checkBox1texture.Checked) main.r.useTexture = true;
            else main.r.useTexture = false;
            if (auto) main.Upd();
        }

        private void checkBox1web_CheckedChanged(object sender, EventArgs e)
        {
            if (main.r == null) return;
            if (checkBox1web.Checked) main.r.polygonWeb = true;
            else main.r.polygonWeb = false;
            if (auto) main.Upd();
        }

        private void checkBox1nolight_CheckedChanged(object sender, EventArgs e)
        {
            if (main.r == null) return;
            if (checkBox1nolight.Checked) main.r.noLight = true;
            else main.r.noLight = false;
            if (auto) main.Upd();
        }

        private void checkBox1norm_CheckedChanged(object sender, EventArgs e)
        {
            if (main.r == null) return;
            if (checkBox1norm.Checked) main.r.midNormales = true;
            else main.r.midNormales = false;
            if (auto) main.Upd();
        }

        private void checkBox1shadow_CheckedChanged(object sender, EventArgs e)
        {
            if (main.r == null) return;
            if (checkBox1shadow.Checked) main.r.shadow = true;
            else main.r.shadow = false;
            if (auto) main.Upd();
        }

        private void checkBox1background_CheckedChanged(object sender, EventArgs e)
        {
            if (main.r == null) return;
            if (checkBox1background.Checked)
            {
                main.r.ObjBackground();
                if (auto) main.Upd();
            }
            else checkBox1background.Checked = true;
        }

        private void radioButton1phong_CheckedChanged(object sender, EventArgs e)
        {
            if (main.r == null) return;
            if (radioButton1phong.Checked) main.r.ambient_occlusion = false;
        }

        private void radioButton2ambient_CheckedChanged(object sender, EventArgs e)
        {
            if (main.r == null) return;
            if (radioButton2ambient.Checked) main.r.ambient_occlusion = true;
            if (auto) main.Upd();
        }

        private void checkBox1optimization_CheckedChanged(object sender, EventArgs e)
        {
            if (main.r == null) return;
            if (checkBox1optimization.Checked) main.r.optimization = true;
            else main.r.optimization = false;
            if (auto) main.Upd();
        }

        private void checkBox1indent_CheckedChanged(object sender, EventArgs e)
        {
            if (main.r == null) return;
            if (checkBox1indent.Checked) main.r.indent = 0.0001;
            else main.r.indent = 0;
            if (auto) main.Upd();
        }

        private void numericUpDown1numlights_ValueChanged(object sender, EventArgs e)
        {
            if (main.r == null) return;
            main.r.lightsNum = (int)numericUpDown1numlights.Value;
            if (auto) main.Upd();
        }

        private void numericUpDown1step_ValueChanged(object sender, EventArgs e)
        {
            if (main.r == null) return;
            main.r.quality = (double)numericUpDown1step.Value;
            if (auto) main.Upd();
        }

        private void numericUpDown1brightness_ValueChanged(object sender, EventArgs e)
        {
            if (main.r == null) return;
            main.r.brightness = (double)numericUpDown1brightness.Value;
            if (auto) main.Upd();
        }
        

        private void button1server_Click(object sender, EventArgs e)
        {
            if (button1server.Text == "Start server")
            {
                server = new RenderingServer();
                Task.Run(() => { server.Run(); });

                foreach (Control item in Controls)
                {
                    item.Enabled = false;
                }
                groupBox12.Enabled = true;
                button1client.Enabled = false;
                textBox1ip.Enabled = false;
                label4.Enabled = false;
                label5.Enabled = false;
                numericUpDown1lightsforserver.Enabled = false;
                button1server.Text = "Stop server";
            }
            else
            {
                server.Stop();

                foreach (Control item in Controls)
                {
                    item.Enabled = true;
                }
                groupBox12.Enabled = true;
                button1client.Enabled = true;
                textBox1ip.Enabled = true;
                label4.Enabled = true;
                label5.Enabled = true;
                numericUpDown1lightsforserver.Enabled = true;
                button1server.Text = "Start server";
            }
        }

        private void comboBox1_TextUpdate(object sender, EventArgs e)
        {
            main.r.opencl = Convert.ToInt32(comboBox1opencl.Text);
        }

        private void comboBox2cpu_TextUpdate(object sender, EventArgs e)
        {
            main.r.cpu = Convert.ToInt32(comboBox2cpu.Text);
        }

        private void button1client_Click(object sender, EventArgs e)
        {
            if (button1client.Text == "Start client")
            {
                try
                {
                    button3run_Click(null, null);
                    main.r.ConnectRenderServer(textBox1ip.Text);
                    button1client.Text = "Stop client";
                    button1server.Enabled = false;
                    
                }
                catch
                {
                    MessageBox.Show("Can't connect server");
                }
            }
            else if (main.r != null)
            {
                main.r.netMode = false;
                main.r.StopClient();
                button1client.Text = "Start client";
                button1server.Enabled = true;
            }
        }

        private void numericUpDown1lightsforserver_ValueChanged(object sender, EventArgs e)
        {
            if (main.r!=null)
            {
                main.r.lightsForServer = (int)numericUpDown1lightsforserver.Value; 
            }
        }
    }
}
