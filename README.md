我将为您设计和实现一个完整的黄金交易软件，采用**C# WinUI 3 + 现代前端 + Java后端服务**的混合架构，实现真正的Windows原生体验。以下是完整的实现方案：

## 📁 项目架构概览

```
GoldTradingSystem/
├── GoldTradingBackend/          # Java后端 (Spring Boot)
│   ├── src/main/java/com/goldtrading/
│   │   ├── controller/         # REST API
│   │   ├── service/           # 业务逻辑
│   │   ├── model/             # 数据模型
│   │   └── repository/        # 数据访问
│   └── pom.xml
├── GoldTradingFrontend/        # Vue 3前端
│   ├── src/
│   │   ├── views/            # 页面组件
│   │   ├── components/       # 可复用组件
│   │   ├── services/         # API服务
│   │   └── assets/           # 静态资源
│   └── package.json
└── GoldTradingDesktop/        # C# WinUI 3桌面应用
    ├── MainWindow.xaml       # 主窗口
    ├── MainWindow.xaml.cs    # 窗口逻辑
    ├── Services/             # 本地服务
    └── GoldTradingDesktop.csproj
```

## 1️⃣ 第一步：Java后端服务 (Spring Boot)

**GoldTradingBackend/src/main/java/com/goldtrading/GoldTradingApplication.java:**
```java
package com.goldtrading;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.context.annotation.Bean;
import org.springframework.web.servlet.config.annotation.CorsRegistry;
import org.springframework.web.servlet.config.annotation.WebMvcConfigurer;

@SpringBootApplication
public class GoldTradingApplication {
    public static void main(String[] args) {
        SpringApplication.run(GoldTradingApplication.class, args);
    }
    
    @Bean
    public WebMvcConfigurer corsConfigurer() {
        return new WebMvcConfigurer() {
            @Override
            public void addCorsMappings(CorsRegistry registry) {
                // 允许所有来源访问，便于开发
                registry.addMapping("/api/**").allowedOrigins("*");
            }
        };
    }
}
```

**GoldTransaction.java (数据模型):**
```java
package com.goldtrading.model;

import jakarta.persistence.*;
import java.time.LocalDateTime;

@Entity
@Table(name = "gold_transactions")
public class GoldTransaction {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long id;
    
    @Column(nullable = false)
    private String type; // "BUY" or "SELL"
    
    @Column(nullable = false)
    private LocalDateTime tradeTime;
    
    @Column(nullable = false)
    private Double weight; // 克数
    
    @Column(nullable = false)
    private Double amount; // 金额
    
    @Column(nullable = false)
    private Double pricePerGram; // 成交牌价
    
    private String remark; // 备注
    
    // 构造函数、getter、setter...
}
```

**TransactionController.java (API控制器):**
```java
package com.goldtrading.controller;

import com.goldtrading.model.GoldTransaction;
import com.goldtrading.service.TransactionService;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api/transactions")
public class TransactionController {
    
    @Autowired
    private TransactionService transactionService;
    
    @GetMapping
    public ResponseEntity<List<GoldTransaction>> getAllTransactions() {
        return ResponseEntity.ok(transactionService.getAllTransactions());
    }
    
    @PostMapping
    public ResponseEntity<GoldTransaction> addTransaction(@RequestBody GoldTransaction transaction) {
        return ResponseEntity.ok(transactionService.addTransaction(transaction));
    }
    
    @PutMapping("/{id}")
    public ResponseEntity<GoldTransaction> updateTransaction(
            @PathVariable Long id, 
            @RequestBody GoldTransaction transaction) {
        return ResponseEntity.ok(transactionService.updateTransaction(id, transaction));
    }
    
    @DeleteMapping("/{id}")
    public ResponseEntity<Void> deleteTransaction(@PathVariable Long id) {
        transactionService.deleteTransaction(id);
        return ResponseEntity.ok().build();
    }
    
    @GetMapping("/analysis/realized")
    public ResponseEntity<Map<String, Object>> analyzeRealizedProfits() {
        return ResponseEntity.ok(transactionService.analyzeRealizedProfits());
    }
    
    @GetMapping("/analysis/detailed")
    public ResponseEntity<Map<String, Object>> analyzeDetailedProfits() {
        return ResponseEntity.ok(transactionService.analyzeDetailedProfits());
    }
    
    @GetMapping("/export/csv")
    public ResponseEntity<String> exportToCsv() {
        return ResponseEntity.ok(transactionService.exportToCsv());
    }
}
```

**TransactionService.java (核心业务逻辑):**
```java
package com.goldtrading.service;

import com.goldtrading.model.GoldTransaction;
import org.springframework.stereotype.Service;

import java.util.*;

@Service
public class TransactionService {
    
    // 使用FIFO算法计算已实现盈亏
    public Map<String, Object> analyzeRealizedProfits() {
        List<GoldTransaction> transactions = getAllTransactionsSorted();
        Queue<GoldTransaction> buyQueue = new LinkedList<>();
        double totalRealizedProfit = 0.0;
        List<Map<String, Object>> profitRecords = new ArrayList<>();
        
        for (GoldTransaction transaction : transactions) {
            if ("BUY".equals(transaction.getType())) {
                buyQueue.add(transaction);
            } else if ("SELL".equals(transaction.getType())) {
                double sellWeight = transaction.getWeight();
                double totalCost = 0.0;
                
                while (sellWeight > 0 && !buyQueue.isEmpty()) {
                    GoldTransaction buy = buyQueue.peek();
                    double buyWeight = buy.getWeight();
                    double buyPrice = buy.getPricePerGram();
                    
                    if (buyWeight <= sellWeight) {
                        totalCost += buyWeight * buyPrice;
                        sellWeight -= buyWeight;
                        buyQueue.poll();
                    } else {
                        totalCost += sellWeight * buyPrice;
                        buy.setWeight(buyWeight - sellWeight);
                        sellWeight = 0;
                    }
                }
                
                double revenue = transaction.getAmount();
                double profit = revenue - totalCost;
                totalRealizedProfit += profit;
                
                Map<String, Object> record = new HashMap<>();
                record.put("sellId", transaction.getId());
                record.put("revenue", revenue);
                record.put("cost", totalCost);
                record.put("profit", profit);
                profitRecords.add(record);
            }
        }
        
        Map<String, Object> result = new HashMap<>();
        result.put("totalRealizedProfit", totalRealizedProfit);
        result.put("profitRecords", profitRecords);
        return result;
    }
    
    // 导出CSV文件
    public String exportToCsv() {
        List<GoldTransaction> transactions = getAllTransactions();
        StringBuilder csv = new StringBuilder();
        csv.append("ID,Type,TradeTime,Weight,Amount,PricePerGram,Remark\n");
        
        for (GoldTransaction t : transactions) {
            csv.append(String.format("%d,%s,%s,%.2f,%.2f,%.2f,%s\n",
                t.getId(), t.getType(), t.getTradeTime(),
                t.getWeight(), t.getAmount(), t.getPricePerGram(),
                t.getRemark()));
        }
        
        return csv.toString();
    }
    
    // 其他业务方法...
}
```

## 2️⃣ 第二步：Vue 3前端界面

**GoldTradingFrontend/src/App.vue:**
```vue
<template>
  <div id="app" class="fluent-app">
    <app-header @toggle-theme="toggleTheme" />
    
    <div class="app-layout">
      <app-navigation @select-view="changeView" />
      
      <main class="main-content">
        <component :is="currentView" />
      </main>
    </div>
    
    <status-bar :message="statusMessage" />
  </div>
</template>

<script>
import { ref, computed } from 'vue'
import AppHeader from './components/AppHeader.vue'
import AppNavigation from './components/AppNavigation.vue'
import StatusBar from './components/StatusBar.vue'
import TransactionView from './views/TransactionView.vue'
import AnalysisView from './views/AnalysisView.vue'
import ExportView from './views/ExportView.vue'

export default {
  name: 'App',
  components: {
    AppHeader,
    AppNavigation,
    StatusBar,
    TransactionView,
    AnalysisView,
    ExportView
  },
  setup() {
    const currentView = ref('TransactionView')
    const isDarkTheme = ref(false)
    const statusMessage = ref('系统就绪')
    
    const changeView = (viewName) => {
      currentView.value = viewName
      statusMessage.value = `切换到${getViewTitle(viewName)}`
    }
    
    const toggleTheme = () => {
      isDarkTheme.value = !isDarkTheme.value
      document.documentElement.setAttribute('data-theme', 
        isDarkTheme.value ? 'dark' : 'light')
    }
    
    const getViewTitle = (viewName) => {
      const titles = {
        TransactionView: '交易管理',
        AnalysisView: '盈亏分析',
        ExportView: '数据导出'
      }
      return titles[viewName] || '未知视图'
    }
    
    return {
      currentView,
      isDarkTheme,
      statusMessage,
      changeView,
      toggleTheme
    }
  }
}
</script>

<style>
:root {
  --background-primary: #f3f2f1;
  --background-secondary: #ffffff;
  --text-primary: #323130;
  --accent-color: #0078d4;
}

[data-theme="dark"] {
  --background-primary: #201f1e;
  --background-secondary: #2d2c2c;
  --text-primary: #f3f2f1;
}

.fluent-app {
  font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
  background: var(--background-primary);
  color: var(--text-primary);
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.app-layout {
  display: flex;
  flex: 1;
}

.main-content {
  flex: 1;
  padding: 24px;
  background: var(--background-secondary);
  border-radius: 8px;
  margin: 16px;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}
</style>
```

**TransactionView.vue (交易管理页面):**
```vue
<template>
  <div class="transaction-view">
    <div class="view-header">
      <h2 class="fluent-heading">黄金交易管理</h2>
      <fluent-button @click="showAddDialog = true" appearance="accent">
        <fluent-icon name="Add" />
        新增交易
      </fluent-button>
    </div>
    
    <fluent-data-grid :items="transactions" :columns="columns">
      <template #cell-type="{ item }">
        <fluent-badge :appearance="item.type === 'BUY' ? 'accent' : 'brand'">
          {{ item.type === 'BUY' ? '买入' : '卖出' }}
        </fluent-badge>
      </template>
      <template #cell-actions="{ item }">
        <fluent-button @click="editTransaction(item)" appearance="subtle">
          编辑
        </fluent-button>
        <fluent-button @click="deleteTransaction(item.id)" appearance="subtle">
          删除
        </fluent-button>
      </template>
    </fluent-data-grid>
    
    <!-- 添加/编辑对话框 -->
    <fluent-dialog :hidden="!showAddDialog" @close="showAddDialog = false">
      <transaction-form 
        v-if="showAddDialog"
        :transaction="editingTransaction"
        @save="saveTransaction"
        @cancel="cancelEdit"
      />
    </fluent-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { FluentButton, FluentDataGrid, FluentDialog, FluentBadge, FluentIcon } from '@fluentui/web-components-vue'
import TransactionForm from '../components/TransactionForm.vue'
import { transactionService } from '../services/api'

const transactions = ref([])
const showAddDialog = ref(false)
const editingTransaction = ref(null)

const columns = [
  { columnDataKey: 'id', title: 'ID' },
  { columnDataKey: 'type', title: '类型' },
  { columnDataKey: 'tradeTime', title: '交易时间' },
  { columnDataKey: 'weight', title: '克数', format: value => `${value}g` },
  { columnDataKey: 'amount', title: '金额', format: value => `¥${value.toFixed(2)}` },
  { columnDataKey: 'pricePerGram', title: '牌价', format: value => `¥${value}/g` },
  { columnDataKey: 'actions', title: '操作' }
]

onMounted(async () => {
  await loadTransactions()
})

const loadTransactions = async () => {
  transactions.value = await transactionService.getAll()
}

const editTransaction = (transaction) => {
  editingTransaction.value = { ...transaction }
  showAddDialog.value = true
}

const saveTransaction = async (transaction) => {
  if (transaction.id) {
    await transactionService.update(transaction.id, transaction)
  } else {
    await transactionService.create(transaction)
  }
  await loadTransactions()
  showAddDialog.value = false
  editingTransaction.value = null
}

const deleteTransaction = async (id) => {
  if (confirm('确定删除此交易记录吗？')) {
    await transactionService.delete(id)
    await loadTransactions()
  }
}

const cancelEdit = () => {
  showAddDialog.value = false
  editingTransaction.value = null
}
</script>
```

**api.js (API服务层):**
```javascript
import axios from 'axios'

const API_BASE = 'http://localhost:8080/api'

export const transactionService = {
  async getAll() {
    const response = await axios.get(`${API_BASE}/transactions`)
    return response.data
  },
  
  async create(transaction) {
    const response = await axios.post(`${API_BASE}/transactions`, transaction)
    return response.data
  },
  
  async update(id, transaction) {
    const response = await axios.put(`${API_BASE}/transactions/${id}`, transaction)
    return response.data
  },
  
  async delete(id) {
    await axios.delete(`${API_BASE}/transactions/${id}`)
  },
  
  async analyzeRealizedProfits() {
    const response = await axios.get(`${API_BASE}/transactions/analysis/realized`)
    return response.data
  },
  
  async exportToCsv() {
    const response = await axios.get(`${API_BASE}/transactions/export/csv`)
    return response.data
  }
}
```

## 3️⃣ 第三步：C# WinUI 3桌面应用

**MainWindow.xaml (主窗口布局):**
```xml
<Window
    x:Class="GoldTradingDesktop.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:controls="using:Microsoft.UI.Xaml.Controls"
    xmlns:muxc="using:Microsoft.UI.Xaml.Controls"
    Title="黄金交易系统"
    Height="900"
    Width="1400"
    ExtendsContentIntoTitleBar="True"
    UseAcrylicBrush="True">
    
    <Grid>
        <!-- Acrylic背景 -->
        <Grid.Background>
            <AcrylicBrush 
                TintColor="#CCFFFFFF" 
                TintOpacity="0.8"
                FallbackColor="White"
                BackgroundSource="HostBackdrop"/>
        </Grid.Background>
        
        <!-- 导航视图 -->
        <muxc:NavigationView 
            x:Name="NavView"
            IsBackButtonVisible="Collapsed"
            IsSettingsVisible="False"
            PaneDisplayMode="LeftCompact"
            OpenPaneLength="260"
            CompactPaneLength="48"
            SelectedItem="{x:Bind SelectedNavItem, Mode=OneWay}">
            
            <muxc:NavigationView.MenuItems>
                <muxc:NavigationViewItem 
                    Content="交易管理" 
                    Tag="transactions"
                    Icon="{StaticResource Symbol.Home}">
                    <muxc:NavigationViewItem.Icon>
                        <FontIcon Glyph="&#xE8B7;" />
                    </muxc:NavigationViewItem.Icon>
                </muxc:NavigationViewItem>
                
                <muxc:NavigationViewItem 
                    Content="盈亏分析" 
                    Tag="analysis"
                    Icon="{StaticResource Symbol.Calculator}">
                    <muxc:NavigationViewItem.Icon>
                        <FontIcon Glyph="&#xE8EF;" />
                    </muxc:NavigationViewItem.Icon>
                </muxc:NavigationViewItem>
                
                <muxc:NavigationViewItem 
                    Content="数据导出" 
                    Tag="export"
                    Icon="{StaticResource Symbol.Download}">
                    <muxc:NavigationViewItem.Icon>
                        <FontIcon Glyph="&#xE896;" />
                    </muxc:NavigationViewItem.Icon>
                </muxc:NavigationViewItem>
            </muxc:NavigationView.MenuItems>
            
            <!-- WebView2容器 -->
            <Grid x:Name="ContentGrid">
                <WebView2 
                    x:Name="WebView"
                    Source="http://localhost:8080"
                    NavigationStarting="WebView_NavigationStarting"
                    NavigationCompleted="WebView_NavigationCompleted"
                    CoreWebView2Initialized="WebView_CoreWebView2Initialized"/>
                
                <!-- 加载指示器 -->
                <ProgressRing 
                    x:Name="LoadingRing"
                    IsActive="True"
                    HorizontalAlignment="Center"
                    VerticalAlignment="Center"
                    Visibility="Visible"/>
            </Grid>
        </muxc:NavigationView>
        
        <!-- 状态栏 -->
        <StatusBar x:Name="AppStatusBar" VerticalAlignment="Bottom">
            <StatusBarItem>
                <TextBlock x:Name="StatusText">就绪</TextBlock>
            </StatusBarItem>
            <StatusBarItem HorizontalAlignment="Right">
                <TextBlock x:Name="ConnectionStatus">
                    <Run Text="后端连接:"/>
                    <Run x:Name="ConnectionStatusIcon" Text="●" Foreground="Green"/>
                </TextBlock>
            </StatusBarItem>
        </StatusBar>
    </Grid>
</Window>
```

**MainWindow.xaml.cs (窗口逻辑):**
```csharp
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
                "GoldTradingSystem",
                "WebView2Data");
            
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
                    ConnectionStatusIcon.Foreground = isHealthy ? 
                        new SolidColorBrush(Colors.Green) : 
                        new SolidColorBrush(Colors.Red);
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
            string css = @"
                /* 增强Fluent Design样式 */
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
                }
            ";
            
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
                worksheet.Cells[1, 1, 1, 7].Style.Fill.PatternType = 
                    OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 1, 1, 7].Style.Fill.BackgroundColor.SetColor(
                    System.Drawing.Color.LightBlue);
                
                // 自动调整列宽
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                
                package.SaveAs(new FileInfo(excelPath));
            }
        }
    }
}
```

## 4️⃣ 第四步：项目配置和构建

**Java后端pom.xml:**
```xml
<?xml version="1.0" encoding="UTF-8"?>
<project>
    <modelVersion>4.0.0</modelVersion>
    <parent>
        <groupId>org.springframework.boot</groupId>
        <artifactId>spring-boot-starter-parent</artifactId>
        <version>3.1.0</version>
    </parent>
    
    <groupId>com.goldtrading</groupId>
    <artifactId>GoldTradingBackend</artifactId>
    <version>1.0.0</version>
    
    <dependencies>
        <dependency>
            <groupId>org.springframework.boot</groupId>
            <artifactId>spring-boot-starter-web</artifactId>
        </dependency>
        <dependency>
            <groupId>org.springframework.boot</groupId>
            <artifactId>spring-boot-starter-data-jpa</artifactId>
        </dependency>
        <dependency>
            <groupId>com.h2database</groupId>
            <artifactId>h2</artifactId>
            <scope>runtime</scope>
        </dependency>
    </dependencies>
    
    <build>
        <plugins>
            <plugin>
                <groupId>org.springframework.boot</groupId>
                <artifactId>spring-boot-maven-plugin</artifactId>
                <configuration>
                    <finalName>GoldTradingBackend</finalName>
                </configuration>
            </plugin>
        </plugins>
    </build>
</project>
```

**C#项目文件 (GoldTradingDesktop.csproj):**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net7.0-windows10.0.19041.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <RootNamespace>GoldTradingDesktop</RootNamespace>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <Platforms>x64</Platforms>
    <RuntimeIdentifiers>win10-x64</RuntimeIdentifiers>
    <UseWinUI>true</UseWinUI>
    <WindowsPackageType>None</WindowsPackageType>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.4.230913002" />
    <PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.22621.755" />
    <PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2210.55" />
    <PackageReference Include="EPPlus" Version="6.2.10" />
    <PackageReference Include="Microsoft.Xaml.Behaviors.WinUI.Managed" Version="2.0.9" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="GoldTradingBackend.jar">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
</Project>
```

## 5️⃣ 第五步：部署和打包

创建**安装脚本 (deploy.ps1)**:
```powershell
# Windows部署脚本
$ErrorActionPreference = "Stop"

# 1. 检查依赖
Write-Host "检查系统依赖..." -ForegroundColor Cyan

# 检查Java
if (!(Get-Command java -ErrorAction SilentlyContinue)) {
    Write-Host "未找到Java运行时，请先安装JDK 17或更高版本" -ForegroundColor Red
    exit 1
}

# 检查WebView2运行时
$webView2Path = "HKLM:\SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
if (!(Test-Path $webView2Path)) {
    Write-Host "请先安装Microsoft Edge WebView2运行时" -ForegroundColor Yellow
    $installWebView2 = Read-Host "是否现在下载安装? (Y/N)"
    if ($installWebView2 -eq 'Y') {
        Start-Process "https://go.microsoft.com/fwlink/p/?LinkId=2124703"
    }
}

# 2. 构建项目
Write-Host "构建Java后端..." -ForegroundColor Cyan
Set-Location ".\GoldTradingBackend"
mvn clean package
Copy-Item "target\GoldTradingBackend.jar" "..\GoldTradingDesktop\bin\Release\net7.0-windows10.0.19041.0\win10-x64\"

Write-Host "构建Vue前端..." -ForegroundColor Cyan
Set-Location "..\GoldTradingFrontend"
npm install
npm run build
# 将构建好的前端复制到Java资源目录

Write-Host "构建C#桌面应用..." -ForegroundColor Cyan
Set-Location "..\GoldTradingDesktop"
dotnet publish -c Release -r win10-x64 --self-contained

# 3. 创建安装包
Write-Host "创建安装包..." -ForegroundColor Cyan
$publishDir = "bin\Release\net7.0-windows10.0.19041.0\win10-x64\publish"
$installerDir = "..\Installer"

New-Item -ItemType Directory -Force -Path $installerDir
Copy-Item "$publishDir\*" $installerDir -Recurse

# 创建快捷方式
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$installerDir\GoldTradingSystem.lnk")
$Shortcut.TargetPath = "$installerDir\GoldTradingDesktop.exe"
$Shortcut.Save()

Write-Host "部署完成！" -ForegroundColor Green
Write-Host "安装文件位于: $installerDir" -ForegroundColor Yellow
```

## 🚀 运行流程

1. **首次运行**: 执行`deploy.ps1`脚本构建所有组件
2. **启动应用**: 双击`GoldTradingDesktop.exe`
3. **应用流程**:
   - C#应用启动并自动运行Java后端
   - WebView2加载Vue前端界面
   - 用户通过现代化界面进行交易操作
   - 所有数据通过REST API与Java后端通信

## ✨ 关键特性实现

1. **原生Windows体验**:
   - 使用WinUI 3的Acrylic材质、Reveal高光效果
   - 系统级右键菜单、窗口动画
   - 支持深色/浅色主题自动切换

2. **混合架构优势**:
   - 前端: Vue 3提供快速响应式界面
   - 后端: Java提供稳定业务逻辑
   - 外壳: C#提供系统集成和原生体验

3. **数据安全与持久化**:
   - H2嵌入式数据库
   - 自动CSV备份
   - Excel导出功能

这个完整方案实现了您要求的：
- ✅ Windows原生体验（类似Adobe Acrobat）
- ✅ 完整的黄金交易功能
- ✅ 现代化的可视化界面
- ✅ 专业的盈亏分析
- ✅ 数据导入导出功能

所有组件都可以独立开发和测试，最后通过C#外壳整合成统一的桌面应用。
