#include <stdio.h>  // 引入标准输入输出库 
#include <math.h>   // 引入数学库，用于计算平方根 
 
// 判断一个数是否为素数的函数，是素数返回1，不是返回0 
int isPrime(int n) {
    int i;
    if (n < 2) {  // 如果n小于2，不是素数 
        return 0;
    }
    if (n == 2) {  // 如果n等于2，是素数 
        return 1;
    }
    if (n % 2 == 0) {  // 如果n是偶数且不等于2，不是素数 
        return 0;
    }
    for (i = 3; i <= sqrt(n); i += 2) {  // 从3开始，每次增加2，检查是否有因子 
        if (n % i == 0) {  // 如果n能被i整除，不是素数 
            return 0;
        }
    }
    return 1;  // 如果没有找到任何因子，n是素数 
}
 
int main() {  // 主函数，程序的入口点 
    int num;  // 声明一个整数变量num 
 
    printf("请输入一个整数: ");  // 提示用户输入一个整数 
    scanf("%d", &num);  // 读取用户输入的整数并存储在变量num中 
 
    if (isPrime(num)) {  // 调用isPrime函数，如果返回1（素数）
        printf("%d 是素数\n", num);  // 输出num是素数 
    } else {  // 如果返回0（非素数）
        printf("%d 不是素数\n", num);  // 输出num不是素数 
    }
 
    return 0;  // 程序正常结束，返回0 
}