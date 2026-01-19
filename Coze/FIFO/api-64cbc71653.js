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

  async analyzeDetailedProfits() {
    const response = await axios.get(`${API_BASE}/transactions/analysis/detailed`)
    return response.data
  },

  async exportToCsv() {
    const response = await axios.get(`${API_BASE}/transactions/export/csv`)
    return response.data
  }
}