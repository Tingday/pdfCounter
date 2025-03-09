using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;


namespace PDF页数统计
{
    using PdfSharp.Pdf;
    using PdfSharp.Pdf.IO;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public partial class MainWindow : Window
    {
        int yeshu = 0;
        double danjia = 0.05;
        double fujiafei = 0;
        double zongjia = 0;
        
        //路径集合
        public ObservableCollection<string> FilePaths { get; set; }
        //停止异步内部变量
        private CancellationTokenSource _cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
            //初始化
            FilePaths = new ObservableCollection<string>();
            GridWenJian.ItemsSource = FilePaths;
            ButtonTingZhi.IsEnabled = false;
            TextBoxDanJia.Text = danjia.ToString();
            TextBoxCount.Text = yeshu.ToString();
            TextBoxFuJia.Text = fujiafei.ToString();
            zongjia = danjia * yeshu + fujiafei;
            TextBoxZongJia.Text = zongjia.ToString();
        }

        //计算页数的函数
        static int PageCount(String filePath)
        {
            try
            {
                PdfDocument document = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
                return document.PageCount;
            }catch(Exception ex)
            {
                Console.WriteLine("Error calculating page count: " + ex.Message);
                return -1; // 返回 -1 表示失败
            }
            
        }

        private void TextBoxFuJia_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!(TextBoxFuJia.Text == string.Empty))
            {
                //这里改变也就是准备调整附加费了
                fujiafei = int.Parse(TextBoxFuJia.Text);
                zongjia = danjia * yeshu + fujiafei;
                TextBoxZongJia.Text = zongjia.ToString();
            }
        }
        private void TextBoxDanJia_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(TextBoxDanJia.Text == "") return;   
            //这里改变也就是调整了单价，总价也要改变
            danjia = double.Parse(TextBoxDanJia.Text);
            zongjia = danjia * yeshu + fujiafei;
            TextBoxZongJia.Text = zongjia.ToString();
        }
        //使用async函数启动线程
        private async void ButtonTongJi_Click(object sender, RoutedEventArgs e)
        {
            //初始化
            buttonTongJi.IsEnabled = false;
            ButtonTingZhi.IsEnabled = true;
            TextBoxCount.Text = "0";
            TextBoxZongJia.Text = "0";
            _cancellationTokenSource = new CancellationTokenSource();

            //启动异步计算，Task.Run可以避免进程阻塞
            try
            {
                await Task.Run(() => CalculateYeShu(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
                MessageBox.Show("统计完成", "信息");
            }
            catch(OperationCanceledException)
            {
                //任务取消，显示取消信息
                MessageBox.Show("统计任务已取消！", "信息");
            }
            finally
            {
                ButtonTingZhi.IsEnabled = false;
                buttonTongJi.IsEnabled = true;
            }

            
        }
        // 重写窗口的OnClosed方法，确保窗口关闭时释放资源
        protected override void OnClosed(EventArgs e)
        {
            _cancellationTokenSource?.Dispose();
            base.OnClosed(e);
        }
        //计算页数并且动态显示在TextBox中
        private void CalculateYeShu(CancellationToken cancellationToken)
        {
            //统计总页数
            yeshu = 0; //清空
            Dispatcher.Invoke(() => ListBoxWenTi.Items.Clear());
            
            foreach (var pdfFilePath in FilePaths)
            {
                //检查是否被取消
                cancellationToken.ThrowIfCancellationRequested();
                //计算总页数
                int pageCount = PageCount(pdfFilePath);
                if(pageCount != -1)
                {
                    yeshu += PageCount(pdfFilePath);
                }
                else
                {
                    //将错误的文件路径添加到listboxWenTi中
                    Dispatcher.Invoke(() => ListBoxWenTi.Items.Add(pdfFilePath));
                }
                //适当延时
                System.Threading.Thread.Sleep(500);
                zongjia = danjia * yeshu + fujiafei;
                //使用Dispatcher可以在线程上显示UI结果。
                Dispatcher.Invoke(()=> TextBoxCount.Text = yeshu.ToString());
                Dispatcher.Invoke(()=> TextBoxZongJia.Text = zongjia.ToString());
            }
        }

        private void GridWenJian_DragEnter(object sender, DragEventArgs e)
        {
            // 检查拖拽的数据是否包含文件
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void GridWenJian_Drop(object sender, DragEventArgs e)
        {
            // 获取拖拽的文件路径
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                //过滤PDF文件
                var pdfFiles = files.Where(file => Path.GetExtension(file).Equals(".pdf", StringComparison.OrdinalIgnoreCase));
                foreach (string file in pdfFiles)
                {
                    // 将文件路径添加到数据源
                    FilePaths.Add(file);
                }
            }
        }

        private void ButtonQingKong_Click(object sender, RoutedEventArgs e)
        {
            FilePaths.Clear();//清空路径集合
        }

        private void GridWenJian_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        //
        private void ButtonYiChu_Click(object sender, RoutedEventArgs e)
        {
            if (GridWenJian.SelectedItem is string selectItem)
            {
                FilePaths.Remove(selectItem);
            }
        }
        //系统默认软件打开
        private void ButtonDaKai_Click(object sender, RoutedEventArgs e)
        {
            if (GridWenJian.SelectedItem is string selectItem)
            {
                if (!string.IsNullOrEmpty(selectItem))
                {
                    string fileName = Path.GetFileNameWithoutExtension(selectItem);
                    MessageBoxResult result = MessageBox.Show($"打开《{fileName}》吗？", "打开", MessageBoxButton.YesNo);
                    if(result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            Process.Start(selectItem);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"无法打开文件: {ex.Message}");
                        }
                    }

                }
            }
        }

        private void TextBoxCount_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        //停止当前异步计算
        private void ButtonTingZhi_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }

        private void ButtonQingKongWenTi_Click(object sender, RoutedEventArgs e)
        {
            //清空异常文件
            ListBoxWenTi.Items.Clear();
        }
        //打开选择的问题
        private void ButtonWenTi(object sender, RoutedEventArgs e)
        {
            if (ListBoxWenTi.SelectedItem != null)
            {
                string pdfFileName = ListBoxWenTi.SelectedItem.ToString();
                try
                {
                    //默认软件打开
                    Process.Start(pdfFileName);

                }catch(Exception ex)
                {
                    MessageBox.Show($"文件无法打开：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("未选择到问题文件！", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        //
        private void ButtonWenTiShanChu(object sender, RoutedEventArgs e)
        {
            if(ListBoxWenTi.SelectedItem != null)
            {
                //删除选中
                ListBoxWenTi.Items.Remove(ListBoxWenTi.SelectedItem);

            }
            else
            {
                MessageBox.Show("未选择到问题文件！", "信息", MessageBoxButton.OK,MessageBoxImage.Information);
            }
        }

        private void ButtonFangWen_Click(object sender, RoutedEventArgs e)
        {
            //访问主页
            string _urlGitHub = "https://github.com/Tingday/pdfCounter";
            // 使用系统默认浏览器打开URL
            try
            {
                Process.Start(_urlGitHub);
            }
            catch (Exception ex)
            {
                // 处理异常，例如浏览器无法启动
                MessageBox.Show("无法打开浏览器: " + ex.Message);
            }

        }
    }
}
