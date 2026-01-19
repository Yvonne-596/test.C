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

<template>
  <div class="transaction-view">
    <h1>黄金交易管理</h1>
    
    <FluentButton @click="() => { editingTransaction.value = null; showAddDialog.value = true }">
      新增交易
    </FluentButton>

    <FluentDataGrid :items="transactions" :columns="columns">
      <template #type="{ item }">
        <FluentBadge :variant="item.type === 'BUY' ? 'success' : 'danger'">
          {{ item.type === 'BUY' ? '买入' : '卖出' }}
        </FluentBadge>
      </template>
      
      <template #actions="{ item }">
        <FluentButton @click="() => editTransaction(item)">编辑</FluentButton>
        <FluentButton @click="() => deleteTransaction(item.id)" variant="danger">删除</FluentButton>
      </template>
    </FluentDataGrid>

    <FluentDialog :open="showAddDialog" @close="cancelEdit">
      <TransactionForm
        :transaction="editingTransaction"
        @save="saveTransaction"
        @cancel="cancelEdit"
      />
    </FluentDialog>
  </div>
</template>

<style scoped>
.transaction-view {
  padding: 20px;
}

h1 {
  margin-bottom: 20px;
  color: var(--text-primary);
}
</style>