using System.Runtime.InteropServices;

namespace SoundComparer.WaveUtils
{
    /// <summary>
    /// Перечисление форматов WAV файлов. 
    /// PCM используется для обычных несжатых аудиофайлов,
    /// а Float для аудиофайлов с плавающей точкой.
    /// </summary>
    public enum WaveFormats
    {
        Pcm = 1,   // Формат PCM
        Float = 3  // Формат с плавающей точкой
    }

    /// <summary>
    /// Класс, представляющий заголовок формата RIFF Wave файла.
    /// Он содержит информацию о параметрах аудио, таких как частота дискретизации,
    /// количество каналов, средняя скорость передачи данных и т.д.
    /// </summary>
	[StructLayout(LayoutKind.Sequential)]
    public class WaveFormat
    {
        #region Members

        // Поле wFormatTag: Определяет формат аудиоданных (например, PCM или Float)
        private short wFormatTag;

        // Поле nChannels: Количество каналов (1 для моно, 2 для стерео и т.д.)
        private short nChannels;

        // Поле nSamplesPerSec: Частота дискретизации (количество выборок в секунду)
        private int nSamplesPerSec;

        // Поле nAvgBytesPerSec: Средняя скорость передачи данных (байт в секунду)
        private int nAvgBytesPerSec;

        // Поле nBlockAlign: Размер блока в байтах
        private short nBlockAlign;

        // Поле wBitsPerSample: Количество бит на выборку
        private short wBitsPerSample;

        // Поле cbSize: Дополнительная информация о формате, используется для расширенных форматов
        private short cbSize;

        #endregion // Members

        #region Properties

        /// <summary>
        /// Поле wFormatTag: Определяет формат аудиоданных (например, PCM или Float).
        /// </summary>
        public short FormatTag
        {
            get { return wFormatTag; }
            set { wFormatTag = value; }
        }

        /// <summary>
        /// Поле nChannels: Определяет количество каналов (моно, стерео и т.д.).
        /// </summary>
        public short Channels
        {
            get { return nChannels; }
            set { nChannels = value; }
        }

        /// <summary>
        /// Поле nSamplesPerSec: Определяет частоту дискретизации (количество выборок в секунду).
        /// </summary>
        public int SamplesPerSec
        {
            get { return nSamplesPerSec; }
            set { nSamplesPerSec = value; }
        }

        /// <summary>
        /// Поле nAvgBytesPerSec: Средняя скорость передачи данных (в байтах за секунду).
        /// </summary>
        public int AvgBytesPerSec
        {
            get { return nAvgBytesPerSec; }
            set { nAvgBytesPerSec = value; }
        }

        /// <summary>
        /// Поле nBlockAlign: Определяет размер блока в байтах.
        /// </summary>
        public short BlockAlign
        {
            get { return nBlockAlign; }
            set { nBlockAlign = value; }
        }

        /// <summary>
        /// Поле wBitsPerSample: Количество бит на выборку.
        /// </summary>
        public short BitsPerSample
        {
            get { return wBitsPerSample; }
            set { wBitsPerSample = value; }
        }

        /// <summary>
        /// Поле cbSize: Дополнительная информация о формате. 
        /// Используется для расширенных форматов WAV.
        /// </summary>
        public short Size
        {
            get { return cbSize; }
            set { cbSize = value; }
        }

        #endregion // Properties

        #region Constructors

        /// <summary>
        /// Конструктор по умолчанию. Инициализирует пустой объект WaveFormat.
        /// </summary>
        public WaveFormat()
        {
            // Конструктор по умолчанию
        }

        /// <summary>
        /// Конструктор с параметрами. Устанавливает частоту дискретизации, количество бит на выборку и количество каналов.
        /// </summary>
        /// <param name="samplesPerSec">Количество выборок в секунду (частота дискретизации).</param>
        /// <param name="bitsPerSample">Количество бит на выборку.</param>
        /// <param name="channels">Количество каналов (моно, стерео и т.д.).</param>
		public WaveFormat(int samplesPerSec, short bitsPerSample, short channels)
        {
            // Устанавливаем формат в PCM
            wFormatTag = (short)WaveFormats.Pcm;

            // Устанавливаем количество каналов
            nChannels = channels;

            // Устанавливаем частоту дискретизации
            nSamplesPerSec = samplesPerSec;

            // Устанавливаем количество бит на выборку
            wBitsPerSample = bitsPerSample;

            // Дополнительные данные отсутствуют (cbSize = 0)
            cbSize = 0;

            // Рассчитываем размер блока (количество байт на выборку для каждого канала)
            nBlockAlign = (short)(channels * (bitsPerSample / 8));

            // Рассчитываем среднюю скорость передачи данных (в байтах за секунду)
            nAvgBytesPerSec = samplesPerSec * nBlockAlign;
        }

        #endregion // Constructors
    }
}
