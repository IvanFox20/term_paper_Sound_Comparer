using System;
using System.Drawing;
using System.Windows.Forms;

namespace SoundComparer.WaveUtils
{
    /// <summary>
    /// Класс для управления визуализацией WAV-файлов.
    /// </summary>
    public class WaveControl : System.Windows.Forms.UserControl
    {
        #region Members

        /// <summary> 
        /// Необходимая переменная для поддержки дизайнера.
        /// </summary>
        private System.ComponentModel.Container components = null;

        /// <summary>
        /// Переменная класса WaveFile, описывающая внутренние структуры .WAV.
        /// </summary>
        private WaveSound m_Wavefile;

        /// <summary>
        /// Логическое значение, указывающее, нужно ли рисовать .WAV. Контрол не рисует .WAV до тех пор, пока он не будет прочитан.
        /// </summary>
        private bool m_DrawWave = false;

        /// <summary>
        /// Строка с именем файла.
        /// </summary>
        //private string			m_Filename;

        /// <summary>
        /// Каждое значение пикселя (по оси X) представляет это количество выборок в WAV-файле.
        /// Начальное значение основано на ширине управления, чтобы .WAV охватывал всю ширину.
        /// </summary>
        private float m_SamplesPerPixel = 0f;

        /// <summary>
        /// Значение, определяющее, насколько увеличивается или уменьшается m_SamplesPerPixel. Это создает эффект "увеличения".
        /// Начальное значение - m_SamplesPerPixel / 25, чтобы оно было пропорционально размеру .WAV.
        /// </summary>
        private float m_ZoomFactor;

        /// <summary>
        /// Начальное значение по оси X для перетаскивания мышью.
        /// </summary>
        private int m_StartX = 0;

        /// <summary>
        /// Конечное значение по оси X для перетаскивания мышью.
        /// </summary>
        private int m_EndX = 0;

        /// <summary>
        /// Смещение от начала волны, где начинается рисование.
        /// </summary>
        private int m_OffsetInSamples = 0;

        #endregion // Members

        #region Properties

        public bool DrawWave
        {
            set { m_DrawWave = value; }
        }

        public WaveSound Sound
        {
            get { return m_Wavefile; }
        }

        private float SamplesPerPixel
        {
            set
            {
                m_SamplesPerPixel = value;
                m_ZoomFactor = m_SamplesPerPixel / 25;
            }
        }

        #endregion // Properties

        #region Constructor

        public WaveControl()
        {
            // Этот вызов необходим для конструктора форм Windows.
            InitializeComponent();
        }

        #endregion // Constructor

        #region Component Designer generated code

        /// <summary> 
        /// Необходимый метод для поддержки конструктора - не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // WaveControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.BackColor = System.Drawing.Color.Lavender;
            this.ForeColor = System.Drawing.Color.Lime;
            this.Name = "WaveControl";
            this.Size = new System.Drawing.Size(530, 101);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.WaveControl_Paint);
            this.ResumeLayout(false);
        }

        /// <summary> 
        /// Освобождает любые используемые ресурсы.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #endregion // Component Designer generated code

        #region Wave Drawing

        /// <summary>
        /// Читает WAV-файл и устанавливает флаг для рисования волны.
        /// </summary>
        /// <param name="wc">Контрол, в который будет загружен WAV-файл.</param>
        /// <param name="filename">Имя файла для чтения.</param>
        /// <param name="progress">Полоса прогресса для отображения состояния загрузки.</param>
        public void Read(WaveControl wc, string filename, ProgressBar progress)
        {
            m_Wavefile = new WaveSound(filename, wc, progress);
            m_Wavefile.ReadWavFile();
            m_DrawWave = true;
        }

        /// <summary>
        /// Переопределяет фон рисования, чтобы избежать нежелательного мерцания.
        /// </summary>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Оставлено пустым, чтобы избежать нежелательного мерцания
        }

        /// <summary>
        /// Рисует волну на графическом контексте.
        /// </summary>
        /// <param name="pea">Аргументы рисования.</param>
        /// <param name="pen">Карандаш для рисования.</param>
        private void Draw(PaintEventArgs pea, Pen pen)
        {
            Graphics grfx = pea.Graphics;
            Rectangle visBounds = ClientRectangle;

            if (m_SamplesPerPixel == 0.0)
            {
                this.SamplesPerPixel = (m_Wavefile.Samples.Length / visBounds.Width);
            }

            grfx.DrawLine(pen, 0, (int)visBounds.Height / 2, (int)visBounds.Width, (int)visBounds.Height / 2);
            Draw16Bit(grfx, pen, visBounds);
        }

        /// <summary>
        /// Рисует 16-битные образцы WAV-файла.
        /// </summary>
        /// <param name="grfx">Графический контекст.</param>
        /// <param name="pen">Карандаш для рисования.</param>
        /// <param name="visBounds">Область видимости.</param>
        private void Draw16Bit(Graphics grfx, Pen pen, RectangleF visBounds)
        {
            int prevX = 0;
            int prevY = 0;

            int i = 0;
            int index = m_OffsetInSamples; // индекс для смещения в массиве данных
            int maxSampleToShow = (int)((m_SamplesPerPixel * visBounds.Width) + m_OffsetInSamples);

            maxSampleToShow = Math.Min(maxSampleToShow, m_Wavefile.Samples.Length);
            while (index < maxSampleToShow)
            {
                short maxVal = -32767;
                short minVal = 32767;

                // Находит максимальные и минимальные пики для этого пикселя 
                for (int x = 0; x < m_SamplesPerPixel; x++)
                {
                    maxVal = Math.Max(maxVal, m_Wavefile.Samples[x + index]);
                    minVal = Math.Min(minVal, m_Wavefile.Samples[x + index]);
                }

                // Масштабирование по высоте окна
                int scaledMinVal = (int)(((minVal + 32768) * visBounds.Height) / 65536);
                int scaledMaxVal = (int)(((maxVal + 32768) * visBounds.Height) / 65536);

                // Если количество образцов на пиксель маленькое или меньше нуля, мы выходим за пределы диапазона увеличения, поэтому ничего не отображаем
                if (m_SamplesPerPixel > 0.0000000001)
                {
                    // Если max/min одинаковы, нарисуйте линию от предыдущей позиции,
                    // в противном случае мы ничего не увидим
                    if (scaledMinVal == scaledMaxVal)
                    {
                        if (prevY != 0)
                            grfx.DrawLine(pen, prevX, prevY, i, scaledMaxVal);
                    }
                    else
                    {
                        grfx.DrawLine(pen, i, scaledMinVal, i, scaledMaxVal);
                    }
                }
                else
                    return;

                prevX = i;
                prevY = scaledMaxVal;

                i++;
                index = (int)(i * m_SamplesPerPixel) + m_OffsetInSamples;
            }
        }

        /// <summary>
        /// Обработчик события рисования для контроля волн.
        /// </summary>
        /// <param name="sender">Объект, вызвавший событие.</param>
        /// <param name="e">Аргументы события рисования.</param>
        private void WaveControl_Paint(object sender, PaintEventArgs e)
        {
            Pen pen = new Pen(Color.Black);
            SolidBrush brush = new SolidBrush(this.BackColor);
            e.Graphics.FillRectangle(brush, 0, 0, (int)e.Graphics.ClipBounds.Width, (int)e.Graphics.ClipBounds.Height);
            if (m_DrawWave)
            {
                Draw(e, pen);
            }

            int regionStartX = Math.Min(m_StartX, m_EndX);
            int regionEndX = Math.Max(m_StartX, m_EndX);

            brush = new SolidBrush(Color.Violet);
            e.Graphics.FillRectangle(brush, regionStartX, 0, regionEndX - regionStartX, (int)e.Graphics.ClipBounds.Height);
        }

        #endregion // Wave Drawing
    }
}
