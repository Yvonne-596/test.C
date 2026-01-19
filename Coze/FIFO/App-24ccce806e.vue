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
      document.documentElement.setAttribute('data-theme', isDarkTheme.value ? 'dark' : 'light')
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