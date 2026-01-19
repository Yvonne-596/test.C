package com.goldtrading.service;

import com.goldtrading.model.GoldTransaction;
import org.springframework.stereotype.Service;

import java.util.*;

@Service
public class TransactionService {
    // 模拟数据存储
    private List<GoldTransaction> transactions = new ArrayList<>();
    private long nextId = 1;

    // 获取所有交易记录
    public List<GoldTransaction> getAllTransactions() {
        return transactions;
    }

    // 获取按时间排序的交易记录
    public List<GoldTransaction> getAllTransactionsSorted() {
        List<GoldTransaction> sorted = new ArrayList<>(transactions);
        sorted.sort(Comparator.comparing(GoldTransaction::getTradeTime));
        return sorted;
    }

    // 添加交易记录
    public GoldTransaction addTransaction(GoldTransaction transaction) {
        transaction.setId(nextId++);
        transactions.add(transaction);
        return transaction;
    }

    // 更新交易记录
    public GoldTransaction updateTransaction(Long id, GoldTransaction transaction) {
        for (int i = 0; i < transactions.size(); i++) {
            if (transactions.get(i).getId().equals(id)) {
                transaction.setId(id);
                transactions.set(i, transaction);
                return transaction;
            }
        }
        return null;
    }

    // 删除交易记录
    public void deleteTransaction(Long id) {
        transactions.removeIf(t -> t.getId().equals(id));
    }

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

    // 详细盈亏分析
    public Map<String, Object> analyzeDetailedProfits() {
        List<GoldTransaction> transactions = getAllTransactionsSorted();
        double totalInvestment = 0.0;
        double totalRevenue = 0.0;
        double currentHoldings = 0.0;

        for (GoldTransaction transaction : transactions) {
            if ("BUY".equals(transaction.getType())) {
                totalInvestment += transaction.getAmount();
                currentHoldings += transaction.getWeight();
            } else if ("SELL".equals(transaction.getType())) {
                totalRevenue += transaction.getAmount();
                currentHoldings -= transaction.getWeight();
            }
        }

        Map<String, Object> result = new HashMap<>();
        result.put("totalInvestment", totalInvestment);
        result.put("totalRevenue", totalRevenue);
        result.put("currentHoldings", currentHoldings);
        result.put("unrealizedProfit", 0.0); // 未实现盈亏需要当前金价
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
                    t.getRemark() != null ? t.getRemark() : ""));
        }

        return csv.toString();
    }
}