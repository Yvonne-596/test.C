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

    // 构造函数
    public GoldTransaction() {}

    public GoldTransaction(String type, LocalDateTime tradeTime, Double weight, Double amount, Double pricePerGram, String remark) {
        this.type = type;
        this.tradeTime = tradeTime;
        this.weight = weight;
        this.amount = amount;
        this.pricePerGram = pricePerGram;
        this.remark = remark;
    }

    // getter和setter
    public Long getId() { return id; }
    public void setId(Long id) { this.id = id; }

    public String getType() { return type; }
    public void setType(String type) { this.type = type; }

    public LocalDateTime getTradeTime() { return tradeTime; }
    public void setTradeTime(LocalDateTime tradeTime) { this.tradeTime = tradeTime; }

    public Double getWeight() { return weight; }
    public void setWeight(Double weight) { this.weight = weight; }

    public Double getAmount() { return amount; }
    public void setAmount(Double amount) { this.amount = amount; }

    public Double getPricePerGram() { return pricePerGram; }
    public void setPricePerGram(Double pricePerGram) { this.pricePerGram = pricePerGram; }

    public String getRemark() { return remark; }
    public void setRemark(String remark) { this.remark = remark; }
}