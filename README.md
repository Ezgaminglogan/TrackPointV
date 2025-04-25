# TrackPointV

<div align="center">
  <img src="Resources/Images/inventory.png" alt="TrackPointV Logo" width="150">
  <h2>Comprehensive Inventory and Sales Management System</h2>

  <!-- Badges -->
  <p>
    <img src="https://img.shields.io/badge/.NET%20MAUI-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET MAUI" />
    <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" alt="C#" />
    <img src="https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white" alt="SQL Server" />
    <img src="https://img.shields.io/badge/MVVM-0078D7?style=for-the-badge&logo=microsoft&logoColor=white" alt="MVVM" />
  </p>
</div>

## 📋 Overview

TrackPointV is a powerful cross-platform point-of-sale (POS) and inventory management application built with .NET MAUI. It helps businesses efficiently track inventory, manage products, process sales, and analyze performance metrics with a modern, responsive interface that works across Windows, macOS, iOS, and Android.

## ✨ Key Features

### 🏷️ Inventory Management
- **Product Tracking**: Easily add, edit, and manage product information
- **Low Stock Alerts**: Visual indicators for products below threshold levels
- **Categorization**: Organize products for efficient management
- **Search & Filter**: Quickly locate products using powerful search tools

### 💲 Sales Processing
- **Intuitive POS Interface**: User-friendly interface for processing transactions
- **Receipt Generation**: Create and print professional sales receipts
- **Customer Management**: Track customer information and purchase history
- **Payment Processing**: Support for multiple payment methods

### 📊 Reporting & Analytics
- **Sales Dashboard**: Visual overview of sales performance and trends
- **Inventory Reports**: Track stock levels, product turnover, and valuation
- **Revenue Analysis**: Break down revenue by time periods, products, or categories
- **Export Functionality**: Export reports in various formats

### 👥 User Management
- **Role-Based Access**: Control system access with customizable user roles
- **Secure Authentication**: Multiple login options including traditional and Google Sign-In
- **Activity Logging**: Track user actions within the system

### 🔐 Authentication Options
- **Traditional Login**: Username and password authentication
- **Google Sign-In**: Secure third-party authentication
- **Registration**: Simple user registration process

## 🚀 Getting Started

### System Requirements
- Windows 10/11 (for Windows version)
- .NET 6.0 or later
- SQL Server (Express version supported)
- Minimum 4GB RAM, 2GHz processor

### Installation

1. **Clone the repository**:
   ```bash
   git clone https://github.com/yourusername/TrackPointV.git
   ```

2. **Open the solution**:
   - Launch Visual Studio 2022
   - Open `TrackPointV.sln`

3. **Configure the database**:
   ```csharp
   // In Service/Connection.cs, update the connection string:
   private readonly string _connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=E:\Programming\TrackPointV\Database\TrackPoints.mdf;Integrated Security=True";
   ```

4. **Set up Google Authentication** (if using):
   - Create a project in the [Google Developer Console](https://console.developers.google.com/)
   - Configure OAuth credentials and obtain Client ID and Client Secret
   - Update credentials in `MauiProgram.cs`:
   ```csharp
   builder.Services.AddSingleton(new GoogleAuthWindowsService(
       "YOUR_CLIENT_ID.apps.googleusercontent.com",
       "YOUR_CLIENT_SECRET"));
   ```

5. **Build and run the application**:
   - Select your target platform (Windows, Android, iOS, Mac)
   - Press F5 to build and run

## ⚙️ Configuration Options

### Database Setup
To set up and initialize the database:

1. **Create Database Tables**:
   ```sql
   -- Example SQL command for User table
   CREATE TABLE [User] (
     UserId INT PRIMARY KEY IDENTITY(1,1),
     Username NVARCHAR(100) NOT NULL,
     Password NVARCHAR(100) NOT NULL,
     LastLoginDate DATETIME,
     IsGoogleUser BIT DEFAULT 0,
     DisplayName NVARCHAR(100)
   )
   ```

2. **Update Connection String**:
   - Locate the `Service/Connection.cs` file
   - Update the connection string to point to your database

### User Authentication
The default admin credentials are:
- Username: `admin`
- Password: `admin`

> ⚠️ **Important**: Change these credentials after first login for security purposes

## 📱 Screenshots

<div align="center">
  <img src="Screenshots/Screenshot_1.png" alt="Login Screen" width="800">
  <p><em>Secure login interface with Google authentication option</em></p>
  
  <img src="Screenshots/Screenshot_2.png" alt="Registration Screen" width="800">
  <p><em>User registration form with traditional and Google sign-up options</em></p>
  
  <img src="Screenshots/Screenshot_3.png" alt="Dashboard" width="800">
  <p><em>Main dashboard with key metrics and navigation options</em></p>
</div>

## 🏗️ Architecture

TrackPointV follows a structured architecture:

```
TrackPointV/
├── Models/         # Data models and business objects
├── View/           # UI pages and components 
├── Service/        # Business logic and services
├── Resources/      # Images, fonts, and other resources
└── Database/       # Database files and scripts
```

Built using:
- **.NET MAUI**: Cross-platform UI framework
- **C#**: Core business logic implementation
- **SQL Server**: Database backend
- **MVVM Pattern**: Clean separation of concerns
- **Google OAuth**: Third-party authentication

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🤝 Support & Contributing

### Getting Help
For support or bug reports, please [open an issue](https://github.com/yourusername/TrackPointV/issues) on GitHub.

### Contributing
Contributions are welcome! To contribute:

1. Fork the project
2. Create your feature branch: `git checkout -b feature/amazing-feature`
3. Commit your changes: `git commit -m 'Add some amazing feature'`
4. Push to the branch: `git push origin feature/amazing-feature`
5. Open a Pull Request

---

<div align="center">
  <p>Developed with ❤️ by TrackPointV Team</p>
</div>
