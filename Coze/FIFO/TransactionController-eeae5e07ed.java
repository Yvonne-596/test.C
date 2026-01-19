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
            @PathVariable Long id, @RequestBody GoldTransaction transaction) {
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