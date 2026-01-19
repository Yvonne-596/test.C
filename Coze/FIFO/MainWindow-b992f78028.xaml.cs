using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;

namespace GoldTradingDesktop
{
    public sealed partial class MainWindow : Window
    {
        private Process javaProcess;
        private HttpClient httpClient;
        private readonly string jarPath = "GoldTradingBackend.jar";

        public MainWindow()
        {
            InitializeComponent();
            InitializeWindow();
            InitializeAsync();
        }

        private void InitializeWindow()
        {
            // 设置窗口样式
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            // 配置UISettings
            var uiSettings = new UISettings();
            uiSettings.ColorValuesChanged += OnSystemThemeChanged;

            // 初始化HttpClient
            httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        }

        private async void InitializeAsync()
        {
            // 初始化WebView2环境
            var env = await CoreWebView2Environment.CreateAsync();
            await WebView.EnsureCoreWebView2Async(env);

            // 配置WebView2
            ConfigureWebView();

            // 启动Java后端
            await StartJavaBackendAsync();

            // 加载前端
            await LoadFrontend();

            // 开始后端健康检查
            StartHealthCheck();
        }

        private void ConfigureWebView()
        {
            // 启用开发者工具（仅调试）
#if DEBUG
            WebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
#endif

            // 禁用默认上下文菜单
            WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

            // 注册JavaScript回调
            WebView.CoreWebView2.AddHostObjectToScript("bridge", new WebViewBridge(this));

            // 设置自定义用户数据文件夹
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GoldTradingSystem", "WebView2Data");
            Directory.CreateDirectory(userDataFolder);
        }

        private async Task StartJavaBackendAsync()
        {
            try
            {
                // 检查后端是否已在运行
                if (await CheckBackendHealth())
                {
                    UpdateStatus("后端服务已运行");
                    return;
                }

                UpdateStatus("正在启动后端服务...");

                // 启动Java进程
                var startInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-jar \"{jarPath}\" --server.port=8080",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                };

                javaProcess = new Process { StartInfo = startInfo };
                javaProcess.Start();

                // 等待服务启动（最多30秒）
                bool isRunning = false;
                for (int i = 0; i < 30; i++)
                {
                    await Task.Delay(1000);
                    if (await CheckBackendHealth())
                    {
                        isRunning = true;
                        break;
                    }
                }

                if (!isRunning)
                {
                    throw new Exception("后端服务启动失败");
                }

                UpdateStatus("后端服务启动成功");
            }
            catch (Exception ex)
            {
                ShowErrorDialog("启动失败", $"无法启动后端服务：{ex.Message}");
            }
        }

        private async Task LoadFrontend()
        {
            try
            {
                // 导航到本地服务
                WebView.Source = new Uri("http://localhost:8080");
                LoadingRing.Visibility = Visibility.Visible;

                // 等待页面加载完成
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                ShowErrorDialog("加载失败", $"无法加载前端界面：{ex.Message}");
            }
        }

        private async Task<bool> CheckBackendHealth()
        {
            try
            {
                var response = await httpClient.GetAsync("http://localhost:8080/api/transactions");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async void StartHealthCheck()
        {
            while (true)
            {
                await Task.Delay(5000); // 每5秒检查一次

                var isHealthy = await CheckBackendHealth();
                await DispatcherQueue.TryEnqueue(() =>
                {
                    ConnectionStatusIcon.Foreground = isHealthy ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                    ConnectionStatusIcon.Text = isHealthy ? "●" : "○";
                });
            }
        }

        // WebView2事件处理
        private void WebView_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            LoadingRing.Visibility = Visibility.Visible;
            UpdateStatus($"正在加载：{args.Uri}");
        }

        private void WebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            LoadingRing.Visibility = Visibility.Collapsed;
            UpdateStatus("页面加载完成");

            // 注入自定义CSS以增强Fluent风格
            InjectFluentStyles();
        }

        private void WebView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
        {
            if (args.Exception != null)
            {
                ShowErrorDialog("WebView2初始化失败", args.Exception.Message);
            }
        }

        private void InjectFluentStyles()
        {
            string css = @"/* 增强Fluent Design样式 */
body {
    font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
}

.fluent-card {
    background: var(--background-secondary);
    border-radius: 8px;
    padding: 20px;
    box-shadow: 0 2px 8px rgba(0,0,0,0.08);
    transition: box-shadow 0.3s ease;
}

.fluent-card:hover {
    box-shadow: 0 4px 16px rgba(0,0,0,0.12);
}

.fluent-button {
    border-radius: 4px;
    padding: 8px 16px;
    border: none;
    background: var(--accent-color);
    color: white;
    cursor: pointer;
    transition: background 0.2s ease;
}

.fluent-button:hover {
    background: var(--accent-color-dark);
}";

            WebView.CoreWebView2.ExecuteScriptAsync(
                $"var style = document.createElement('style');" +
                $"style.textContent = `{css}`;" +
                $"document.head.appendChild(style);");
        }

        // 辅助方法
        private void UpdateStatus(string message)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                StatusText.Text = message;
            });
        }

        private async void ShowErrorDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = Content.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private void OnSystemThemeChanged(UISettings sender, object args)
        {
            // 系统主题变化时更新界面
            DispatcherQueue.TryEnqueue(async () =>
            {
                await WebView.CoreWebView2.ExecuteScriptAsync(
                    "document.documentElement.setAttribute('data-theme', " +
                    "window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');");
            });
        }

        // 窗口关闭时清理资源
        private async void Window_Closed(object sender, WindowEventArgs args)
        {
            // 停止健康检查
            // 关闭Java进程
            if (javaProcess != null && !javaProcess.HasExited)
            {
                javaProcess.Kill();
                await javaProcess.WaitForExitAsync();
            }

            httpClient?.Dispose();
        }
    }

    // WebView桥接类，用于C#和JavaScript通信
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class WebViewBridge
    {
        private readonly MainWindow window;

        public WebViewBridge(MainWindow window)
        {
            this.window = window;
        }

        public void ShowNotification(string title, string message)
        {
            window.DispatcherQueue.TryEnqueue(() =>
            {
                // 显示原生Toast通知
                new ToastNotificationBuilder()
                    .AddText(title)
                    .AddText(message)
                    .Show();
            });
        }

        public async Task<string> ExportToExcel()
        {
            try
            {
                // 调用Java后端导出数据
                var response = await window.httpClient.GetStringAsync(
                    "http://localhost:8080/api/transactions/export/csv");

                // 转换为Excel
                var excelPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    $"GoldTrading_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                ConvertCsvToExcel(response, excelPath);
                return excelPath;
            }
            catch (Exception ex)
            {
                return $"导出失败：{ex.Message}";
            }
        }

        private void ConvertCsvToExcel(string csvContent, string excelPath)
        {
            using (var package = new OfficeOpenXml.ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("交易记录");

                // 解析CSV并写入Excel
                var lines = csvContent.Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    var cells = lines[i].Split(',');
                    for (int j = 0; j < cells.Length; j++)
                    {
                        worksheet.Cells[i + 1, j + 1].Value = cells[j];
                    }
                }

                // 应用样式
                worksheet.Cells[1, 1, 1, 7].Style.Font.Bold = true;
                worksheet.Cells[1, 1, 1, 7].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 1, 1, 7].Style.Fill.BackgroundColor.SetColor(
                    System.Drawing.Color.LightBlue);

                // 自动调整列宽
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                package.SaveAs(new FileInfo(excelPath));
            }
        }
    }
}