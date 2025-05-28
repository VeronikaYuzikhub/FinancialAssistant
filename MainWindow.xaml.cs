using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.IO;


namespace WpfAppfinancial_assistant;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private static readonly HttpClient client = new HttpClient();
    private ObservableCollection<Transaction> transactions = new ObservableCollection<Transaction>();
    private decimal balance = 0m; // початковий баланс
    private const string filePath = "transactions.txt"; // файл збереження історій операцій
    private const string balanceFilePath = "balance.txt";
    
    public MainWindow()
    {
        InitializeComponent();
        SetIconFromUrl("https://static8.depositphotos.com/1378583/985/v/450/depositphotos_9856067-stock-illustration-mountain-logo.jpg"); 
        transactionList.ItemsSource = transactions;
        LoadBalanceFromFile();
        LoadTransactionsFromFile(); // завант. збережені операції
        UpdateUI();
    }
    
    private void SetIconFromUrl(string imageUrl)
        {
            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imageUrl, UriKind.Absolute);
                bitmap.EndInit();
                this.Icon = bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження зображення: {ex.Message}", "⚠️");
            }
        }
    
    private void UpdateUI()
    {
        balanceLabel.Content = $"Баланс: {balance} грн";
    }

    private async void LoadExchangeRates(object sender, RoutedEventArgs e)
    {
        string url = "https://open.er-api.com/v6/latest/UAH";

        try
        {
            string response = await client.GetStringAsync(url); // використ глобальний клієнт, він для відправки HTTP-запитів
            var json = JObject.Parse(response);

            if (json["rates"] != null && json["rates"]["USD"] != null && json["rates"]["EUR"] != null)
            {
                float usdRate = json["rates"]["USD"].ToObject<float>();
                float eurRate = json["rates"]["EUR"].ToObject<float>();

                if (usdRate > 0 && eurRate > 0)
                {
                    MessageBox.Show($"Курс валют:\n1 USD = {1 / usdRate} UAH\n1 EUR = {1 / eurRate} UAH", "〽️ Оновлення курсів");
                }
                else
                {
                    MessageBox.Show("Помилка: некоректні дані щодо курсів валют.", "⚠️ Помилка");
                }
            }
            else
            {
                MessageBox.Show("Помилка: відсутні дані щодо курсів валют.", "⚠️ Помилка");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Помилка завантаження даних: {ex.Message}", "⚠️ Помилка");
        }
    }
    
    private void AddTransaction(object sender, RoutedEventArgs e)
    {
        if (decimal.TryParse(amountInput.Text, out decimal amount) && amount > 0)
        {
            var newTransaction = new Transaction
            {
                Date = DateTimeOffset.UtcNow,
                Description = "Поповнення рахунку",
                Amount = amount //зберігає суму, типу decimal
            };

            balance += amount;
            transactions.Insert(0, newTransaction); //додаємо нову транзакцію на початок списку
            SaveBalanceToFile();
            SaveTransactionsToFile();
            UpdateUI();
        }
        else
        {
            MessageBox.Show("Помилка: введіть коректну суму!", "⚠️");
        }
    }

    private void SubtractTransaction(object sender, RoutedEventArgs e)
    {
        if (decimal.TryParse(amountInput.Text, out decimal amount) && amount > 0)
        {
            if (balance >= amount)
            {
                var newTransaction = new Transaction
                {
                    Date = DateTimeOffset.UtcNow,
                    Description = "Списання коштів",
                    Amount = amount
                };

                balance -= amount;
                transactions.Insert(0, newTransaction);
                SaveBalanceToFile();
                SaveTransactionsToFile();
                UpdateUI();
            }
            else
            {
                MessageBox.Show("Помилка: недостатньо коштів на рахунку!", "⚠️");
            }
        }
        else
        {
            MessageBox.Show("Помилка: введіть коректну суму!", "⚠️");
        }
    }
    
    private void SaveBalanceToFile()
    {
        File.WriteAllText(balanceFilePath, balance.ToString());
    }

    private void LoadBalanceFromFile()
    {
        if (File.Exists(balanceFilePath))
        {
            string savedBalance = File.ReadAllText(balanceFilePath);
            if (decimal.TryParse(savedBalance, out decimal loadedBalance))
            {
                balance = loadedBalance;
            }
        }
    }

    private void SaveTransactionsToFile()
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            foreach (var transaction in transactions)
            {
                writer.WriteLine($"{transaction.Date.ToUniversalTime()};{transaction.Description};{transaction.Amount}");
            }
        }
    }

    private void LoadTransactionsFromFile()
    {
        if (File.Exists(filePath))
        {
            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                string[] details = line.Split(';');
                if (details.Length == 3 && decimal.TryParse(details[2], out decimal amount))
                {
                    if (DateTimeOffset.TryParse(details[0], out DateTimeOffset parsedDate)) //якщо формат дати буде вірним то все ок, вона перетвориться і буде викорситана в Transaction 
                    {
                        transactions.Add(new Transaction
                        {
                            Date = parsedDate,
                            Description = details[1],
                            Amount = amount
                        });
                    }
                    else //якщо формат дати невірний - буде помилка
                    {
                        Console.WriteLine($"Помилка завантаження дати: {details[0]}", "⚠️");
                    }
                }
            }
        }
    }
        
    private void ClearPlaceholder(object sender, RoutedEventArgs e)
    {
        if (amountInput.Text == "Введіть суму")
        {
            amountInput.Text = "";
            amountInput.Foreground = Brushes.Black;
        }
    }

    private void RestorePlaceholder(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(amountInput.Text))
        {
            amountInput.Text = "Введіть суму";
            amountInput.Foreground = Brushes.Gray;
        }
    }
    
    private void ClearTransactions(object sender, RoutedEventArgs e)
    {
        transactions.Clear(); // очищаємо список
        File.WriteAllText(filePath, ""); // очищаємо файл
        balance = 0m;
        SaveBalanceToFile();
        UpdateUI();
        MessageBox.Show("Всі транзакції видалені!", "🗑 Видалення");
    }
}

public class Transaction
{
    public DateTimeOffset Date { get; set; }
    public string Description { get; set; }
    public decimal Amount { get; set; }
}

