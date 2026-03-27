using System.Text;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfCalculator
{
    public partial class MainWindow : Window
    {
        // 运算符优先级字典
        private Dictionary<string, int> _priority = new Dictionary<string, int>
        {
            { "(", 0 },
            {"+", 1 },
            {"-", 1 },
            {"*", 2 },
            {"/", 2 },
            {")", 3 }
        };

        public MainWindow()
        {
            InitializeComponent();
        }

        // 数字和小数点事件
        private void numClick(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            string input = btn.Content.ToString();
            string currentText = resultTextBox.Text;

            // 清空错误提示
            if (currentText.StartsWith("错误"))
            {
                resultTextBox.Clear();
                currentText = string.Empty;
            }

            // 处理小数点
            if (input == ".")
            {
                // 1.文本为空，补0
                if (string.IsNullOrEmpty(currentText))
                {
                    resultTextBox.Text += "0.";
                    return;
                }

                // 2.最后一个字符不是小数点
                string lastToken = GetLastToken();
                if (lastToken != ".")
                {
                    resultTextBox.Text += ".";
                    return;
                }

                return;
            }

            resultTextBox.Text += input;
        }

        // 运算符点击事件
        private void optClick(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            string op = btn.Content.ToString();
            string currentText = resultTextBox.Text;

            // 清空错误提示
            if (currentText.StartsWith("错误"))
            {
                resultTextBox.Clear();
                currentText = string.Empty;
            }

            // 空文本时只允许输入负号
            if (string.IsNullOrEmpty(currentText))
            {
                if (op == "-")
                {
                    resultTextBox.Text += op;
                }
                return;
            }

            // 若最后一个字符是运算符，则替换为新的运算符
            char lastChar = currentText[currentText.Length - 1];
            if ("+-*/".Contains(lastChar))
            {
                resultTextBox.Text = currentText.Substring(0, currentText.Length - 1) + op;
                return;
            }

            resultTextBox.Text += op;
        }

        // 清除按钮
        private void resetClick(object sender, RoutedEventArgs e)
        {
            resultTextBox.Clear();
        }

        // 等号事件
        private void eqlClick(object sender, RoutedEventArgs e)
        {
            string expression = resultTextBox.Text.Trim();
            if (string.IsNullOrEmpty(expression))
                return;
            try
            {
                List<string> postfix = InfixToPostfix(expression);
                double result = EvaluatePostfix(postfix);

                // 处理整数显示
                resultTextBox.Text = result % 1 == 0 ? result.ToString("F0") : result.ToString();
            }
            catch (Exception ex)
            {
                resultTextBox.Text = "错误: " + ex.Message;
            }
        }

        // 括号点击事件
        private void brtClick(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            string bracket = btn.Content.ToString();
            resultTextBox.Text += bracket;
        }


        // 中缀表达式转后缀表达式
        private List<string> InfixToPostfix(string expression)
        {
            List<string> postfix = new List<string>();
            Stack<string> opStack = new Stack<string>();
            List<string> tokens = Tokenize(expression);

            foreach (string token in tokens)
            {
                if (double.TryParse(token, out _))
                {
                    postfix.Add(token); // 数字直接入队
                }
                else if (token == "(")
                {
                    opStack.Push(token);
                }
                else if (token == ")")
                {
                    // 弹出运算符直到遇到左括号
                    while (opStack.Count > 0 && opStack.Peek() != "(")
                    {
                        postfix.Add(opStack.Pop());
                    }
                    if (opStack.Count == 0)
                        throw new Exception("缺少左括号");
                    opStack.Pop(); // 弹出左括号（不加入结果）
                }
                else // 处理运算符
                {
                    while (opStack.Count > 0 && opStack.Peek() != "(" &&
                           _priority[opStack.Peek()] >= _priority[token])
                    {
                        postfix.Add(opStack.Pop());
                    }
                    opStack.Push(token);
                }
            }

            while(opStack.Count > 0)
            {
                string op = opStack.Pop();
                if (op == "(")
                    throw new Exception("缺少右括号");
                postfix.Add(op);
            }

            return postfix;
        }

        private List<string> Tokenize(string expression)
        {
            List<string> tokens = new List<string>();
            StringBuilder currentNumber = new StringBuilder();
            bool isNegative = false;

            for (int i = 0; i < expression.Length; i++)
            {
                char c = expression[i];

                // 处理数字和小数点
                if (char.IsDigit(c) || c == '.')
                {
                    if(c == '.')
                    {
                        if(currentNumber.ToString().Contains("."))
                            throw new Exception("数字中不能存在多个小数点");
                        currentNumber.Append(c);
                    }
                    else
                    {
                        // 数字字符正常添加
                        currentNumber.Append(c);
                    }
                }
                else
                {
                    // 先处理当前已收集的数字
                    if (currentNumber.Length > 0)
                    {
                        tokens.Add(isNegative ? $"-{currentNumber}" : currentNumber.ToString());
                        currentNumber.Clear();
                        isNegative = false;
                    }

                    // 处理运算符和括号
                    if ("+-*/()".Contains(c))
                    {
                        // 识别负号
                        if (c == '-' && (i == 0 || "+-*/(".Contains(expression[i - 1])))
                        {
                            isNegative = true;
                        }
                        else
                        {
                            tokens.Add(c.ToString());
                        }
                    }
                    else
                    {
                        throw new Exception($"无效字符: {c}");
                    }
                }
            }
            if (currentNumber.Length > 0)
            {
                tokens.Add(isNegative ? $"-{currentNumber}" : currentNumber.ToString());
            }
            return tokens;
        }

        private double EvaluatePostfix(List<string> postfix)
        {
            Stack<double> stack = new Stack<double>();

            foreach (string token in postfix)
            {
                if (double.TryParse(token, out double num))
                {
                    stack.Push(num);
                }
                else
                {
                    if (stack.Count < 2)
                        throw new Exception("表达式无效");

                    double b = stack.Pop(); // 注意顺序，后弹出的作为右操作数
                    double a = stack.Pop(); // 先弹出的作为左操作数
                    double result = token switch
                    {
                        "+" => a + b,
                        "-" => a - b,
                        "*" => a * b,
                        "/" => b == 0 ? throw new Exception("除以零错误") : a / b,
                        _ => throw new Exception($"未知运算符: {token}"),
                    };
                    stack.Push(result);
                }
            }

            if (stack.Count != 1)
                throw new Exception("表达式无效");

            return stack.Pop();
        }

        // 获取输入框中最后一个token
        private string GetLastToken()
        {
            string text = resultTextBox.Text;
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            int i = text.Length - 1;
            // 从后向前找运算符或括号，确定最后一个数字的范围
            while (i >= 0 && !"+-*/()".Contains(text[i]))
            {
                i--;
            }
            return text.Substring(i + 1);
        }

        // 删除按钮事件
        private void delClick(object sender, RoutedEventArgs e)
        {
            if (resultTextBox == null || string.IsNullOrEmpty(resultTextBox.Text)) {
                return;
            }

            // 若当前是错误提示则清空
            if (resultTextBox.Text.StartsWith("错误"))
            {
                resultTextBox.Clear();
                return;
            }

            // 删除最后一个字符
            resultTextBox.Text = resultTextBox.Text.Substring(0, resultTextBox.Text.Length - 1);
        }
    }
}