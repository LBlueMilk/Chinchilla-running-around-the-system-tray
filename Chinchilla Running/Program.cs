namespace Chinchilla_Running
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // ���åD���f�ñҰʨt�Φ��L�ϼ�
            Application.Run(new Form1());
        }
    }
}
