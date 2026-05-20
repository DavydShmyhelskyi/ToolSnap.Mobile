# ToolSnap.Mobile — .NET MAUI App

ToolSnap.Mobile is a cross-platform mobile application for interacting with the **ToolSnap** tool-management system.  
It enables workers to receive, return, and confirm tool operations directly from their devices.

---

## 📱 Features
- User login and session management
- Issuing and returning tools
- Photo capture (camera & gallery)
- Geolocation submission during operations
- Viewing available tools
- Map view using Leaflet (via WebView)
- Tool dictionary browsing (types, brands, models)
- Integration with the ToolSnap REST API

---

## 🛠 Tech Stack
- **.NET MAUI**
- **XAML UI**
- **HttpClient + System.Text.Json**
- **MAUI Essentials (Geolocation, MediaPicker)**
- **Leaflet.js via WebView**
- **ASP.NET Core API**

---

## 🌍 Supported Platforms
- ✔ **Android** — primary supported platform  
- ✔ **Windows** — fully supported 

(If needed, I can prepare instructions for enabling iOS support.)

---

## ▶️ Run the App
```bash
dotnet restore
dotnet build
dotnet maui run -t windows
# or
dotnet maui run -t android
```

---

## 📁 Key Modules
- `Services/` — API and session handling  
- `Dtos/` — data transfer objects  
- `Models/` — internal models  
- `Pages/` — XAML UI pages  
- `PhotoSessions/` — photo-handling flow  
- `MapPage/` — Leaflet map integration  

---

## 📄 Purpose
The app is used for educational, research, and demonstration purposes within the ToolSnap ecosystem.
