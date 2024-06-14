﻿using System;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Pociag
{
    public partial class Register : Window
    {
        private readonly string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.db");

        public Register()
        {
            InitializeComponent();
            InitializeDatabase();  // Inicjalizujemy bazę danych przy uruchomieniu okna
            GetDiscountList();
        }

        private void InitializeDatabase()
        {
            using (SQLiteConnection conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();

                // Sprawdzanie i tworzenie tabeli Discounts
                string checkDiscountsTableQuery = "SELECT name FROM sqlite_master WHERE type='table' AND name='Discounts';";
                using (SQLiteCommand checkDiscountsCmd = new SQLiteCommand(checkDiscountsTableQuery, conn))
                {
                    var result = checkDiscountsCmd.ExecuteScalar();
                    if (result == null)
                    {
                        string createDiscountsTableQuery = @"
                            CREATE TABLE Discounts (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Description TEXT NOT NULL
                            )";
                        using (SQLiteCommand createDiscountsCmd = new SQLiteCommand(createDiscountsTableQuery, conn))
                        {
                            createDiscountsCmd.ExecuteNonQuery();
                        }
                    }
                }

                // Sprawdzanie i tworzenie tabeli Users
                string checkUsersTableQuery = "SELECT name FROM sqlite_master WHERE type='table' AND name='Users';";
                using (SQLiteCommand checkUsersCmd = new SQLiteCommand(checkUsersTableQuery, conn))
                {
                    var result = checkUsersCmd.ExecuteScalar();
                    if (result == null)
                    {
                        string createUsersTableQuery = @"
                            CREATE TABLE Users (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Username TEXT NOT NULL,
                                Email TEXT NOT NULL,
                                Password TEXT NOT NULL,
                                DiscountId INTEGER NOT NULL,
                                FOREIGN KEY (DiscountId) REFERENCES Discounts(Id)
                            )";
                        using (SQLiteCommand createUsersCmd = new SQLiteCommand(createUsersTableQuery, conn))
                        {
                            createUsersCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text == "Username" ? "" : UsernameTextBox.Text;
            string email = EmailTextBox.Text == "Email" ? "" : EmailTextBox.Text;
            string password = PasswordBox.Password == "Password" ? "" : PasswordBox.Password;
            string selectedDiscount = DiscountComboBox.SelectedItem as string;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(selectedDiscount))
            {
                SetMessage("All fields must be filled in", Colors.Red);
                return;
            }
            if (!IsValidEmail(email))
            {
                SetMessage("Invalid email format.", Colors.Red);
                return;
            }
            if (IsUsernameExists(username))
            {
                SetMessage("Username already exists", Colors.Red);
                return;
            }
            else
            {
                AddUserToDatabase(username, email, password, selectedDiscount);
                SetMessage("Registration completed successfully", Colors.Green);
            }
        }

        private bool IsUsernameExists(string username)
        {
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM Users WHERE Username = @Username";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        private void AddUserToDatabase(string username, string email, string password, string discountDescription)
        {
            int retryCount = 5;
            while (retryCount > 0)
            {
                try
                {
                    using (SQLiteConnection connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                    {
                        connection.Open();

                        // Pobieramy ID zniżki na podstawie opisu
                        int discountId;
                        string discountQuery = "SELECT Id FROM Discounts WHERE Description = @Description";
                        using (SQLiteCommand discountCommand = new SQLiteCommand(discountQuery, connection))
                        {
                            discountCommand.Parameters.AddWithValue("@Description", discountDescription);
                            var result = discountCommand.ExecuteScalar();
                            if (result != null)
                            {
                                discountId = Convert.ToInt32(result);
                            }
                            else
                            {
                                MessageBox.Show("Selected discount not found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }

                        // Dodajemy użytkownika do tabeli Users z DiscountId
                        string query = "INSERT INTO Users (Username, Email, Password, DiscountId) VALUES (@Username, @Email, @Password, @DiscountId)";
                        using (SQLiteCommand command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Username", username);
                            command.Parameters.AddWithValue("@Email", email);
                            command.Parameters.AddWithValue("@Password", password);
                            command.Parameters.AddWithValue("@DiscountId", discountId);

                            command.ExecuteNonQuery();
                            MessageBox.Show("User registered successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                    }
                }
                catch (SQLiteException ex) when (ex.Message.Contains("database is locked"))
                {
                    retryCount--;
                    System.Threading.Thread.Sleep(200); // Odczekaj przed ponowieniem próby
                }
                catch (SQLiteException ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            MessageBox.Show("Operation failed after multiple attempts due to database being locked.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }


        private void GetDiscountList()
        {
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                string query = "SELECT Description FROM Discounts";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DiscountComboBox.Items.Add(reader["Description"].ToString());
                        }
                    }
                }
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void SetMessage(string message, Color color)
        {
            MessageTextBlock.Text = message;
            MessageTextBlock.Foreground = new SolidColorBrush(color);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Login loginWindow = new Login();
            loginWindow.Show();
            this.Close();
        }

        private void Window_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void RemoveText(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                if (textBox.Text == "Username" || textBox.Text == "Email")
                {
                    textBox.Text = "";
                    textBox.Foreground = new SolidColorBrush(Colors.White);
                    textBox.Opacity = 1.0;
                }
            }
        }

        private void AddText(object sender, EventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && string.IsNullOrWhiteSpace(textBox.Text))
            {
                if (textBox.Name == "UsernameTextBox")
                {
                    textBox.Text = "Username";
                }
                else if (textBox.Name == "EmailTextBox")
                {
                    textBox.Text = "Email";
                }
                textBox.Foreground = new SolidColorBrush(Colors.Gray);
                textBox.Opacity = 0.5;
            }
        }

        private void RemovePasswordText(object sender, EventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            if (passwordBox != null && passwordBox.Password == "Password")
            {
                passwordBox.Password = "";
                passwordBox.Foreground = new SolidColorBrush(Colors.White);
                passwordBox.Opacity = 1.0;
            }
        }

        private void AddPasswordText(object sender, EventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            if (passwordBox != null && string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                passwordBox.Password = "Password";
                passwordBox.Foreground = new SolidColorBrush(Colors.Gray);
                passwordBox.Opacity = 0.5;
            }
        }
    }
}
