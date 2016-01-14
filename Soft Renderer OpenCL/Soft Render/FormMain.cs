using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Soft_Renderer
{
    public partial class FormMain : Form
    {

        public Renderer r;
        ControlsForm form;
        Timer progress;

        public FormMain()
        {
            InitializeComponent();

            form = new ControlsForm(this);
            form.Show();

            progress = new Timer();
            progress.Interval = 100;
            progress.Tick += Progress_Tick;
            progress.Start();

        }

        private void Progress_Tick(object sender, System.EventArgs e)
        {
            if (r != null)
            {
                Text = "Renderer (" + r.progress + ")";
            }
            else if (form.server!=null && form.server.r != null)
            {
                Text = "Renderer (" + form.server.r.progress + ")";
            }
        }

        Task task;

        public bool inProcess = false;

        Bitmap img;

        /// <summary>
        /// Вывести новый кадр в окно
        /// </summary>
        public void Upd()
        {
            ControlsForm.fulltime = DateTime.Now.Ticks;
            if (inProcess) return;


            if (task != null && task.Status == TaskStatus.Running) return;
            task = new Task(() =>
            {
                inProcess = true;
                //============================
                img = r.GetFrame();
                img.RotateFlip(RotateFlipType.RotateNoneFlipXY);

                double imgHeight = img.Height > 800 ? 800 : img.Height;
                double imgWidth = imgHeight / img.Height * img.Width;

                Bitmap temp = new Bitmap(img, new Size((int)imgWidth, (int)imgHeight));

                this.Invoke(new Action(() => {

                    Width = (int)imgWidth;
                    Height = (int)imgHeight;
                    pictureBox1.Image = temp;
                    ControlsForm.fulltime = DateTime.Now.Ticks - ControlsForm.fulltime;
                    ShowTime(this, new EventArgs());
                }));
                //============================
                inProcess = false;
            });
            task.Start();


        }



        public event EventHandler ShowTime;


        int i = 0;

        /// <summary>
        /// Сохранить текущий кадр
        /// </summary>
        public void Save()
        {
            if (pictureBox1.Image == null) return;
            i++;
            img.Save("image"+i+".bmp");
        }

    }
}
