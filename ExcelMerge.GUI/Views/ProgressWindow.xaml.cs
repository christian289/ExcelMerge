namespace ExcelMerge.GUI.Views;

/// <summary>
/// ProgressWindow.xaml の相互作用ロジック
/// </summary>
public partial class ProgressWindow : Window
{
    public ProgressWindow()
    {
        InitializeComponent();
    }

    public static void DoWorkWithModal(Action<IProgress<string>> work)
    {
        var window = new ProgressWindow();

        window.Loaded += (_, args) =>
        {
            var worker = new BackgroundWorker();
            var progress = new Progress<string>(data => window.Message.Content = data);

            worker.DoWork += (s, workerArgs) => work(progress);
            worker.RunWorkerCompleted += (s, workerArgs) => window.Close();
            worker.RunWorkerAsync();
        };

        window.ShowDialog();
    }
}
