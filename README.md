# 🤖 AI Code Documenter

![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![Language](https://img.shields.io/badge/Language-C%23-purple)
![AI-Engine](https://img.shields.io/badge/AI-Ollama-orange)

## 📖 Description
**AI Code Documenter** is a powerful Windows desktop application that automates the process of documenting software projects. It takes any GitHub repository URL, analyzes the source code using a local AI model (**Ollama**), and generates a professional, comprehensive PDF report.

It goes beyond simple summaries by generating **Visual Architecture Diagrams**, **Database Schemas**, and **File Trees** automatically.

---

## ✨ Key Features
* **🚀 Automated Analysis**: Clones repositories and analyzes C#, Python, Java, and other code files line-by-line.
* **🧠 Local AI Processing**: Uses **Ollama (qwen2.5-coder)** running locally via Docker—your code never leaves your machine.
* **📊 Visual Diagrams**:
  * **Architecture Class Diagrams**: Automatically inferred from code relationships.
  * **Database ER Diagrams**: Infers schema from Data Access Layer (DAL) code.
  * **Project Tree**: Visual folder structure generation.
* **📄 PDF Export**: Converts the generated HTML report into a polished, shareable PDF.
* **🛡️ Privacy First**: Runs completely offline after the initial model download.

---

## 🛠️ Tech Stack
* **Language**: C# (.NET 6/8 Windows Forms)
* **AI Backend**: [Ollama](https://ollama.com/) (running via Docker)
* **PDF Engine**: [PuppeteerSharp](https://www.puppeteersharp.com/) (Headless Chrome)
* **Formatting**: [Markdig](https://github.com/xoofx/markdig) (Markdown processing)
* **Diagrams**: [Mermaid.js](https://mermaid.js.org/)

---

## ⚙️ Prerequisites
Before running the application, ensure you have the following installed:

1.  **[Docker Desktop](https://www.docker.com/products/docker-desktop/)**: Required to run the local AI server.
2.  **[Git](https://git-scm.com/downloads)**: Required to clone repositories.
3.  **[.NET SDK](https://dotnet.microsoft.com/en-us/download)**: To build and run the C# application.

---

## 🚀 Installation & Setup Guide

### Step 1: Set up the AI Server (Ollama)
The application requires a local AI server running in Docker.

1.  **Install & Open Docker Desktop**.
2.  Open PowerShell (Admin) and run the following command to start the server:
    ```powershell
    docker run -d --gpus=all -v ollama:/root/.ollama -p 11434:11434 --name ai-server ollama/ollama
    ```
    *(Note: If you don't have an NVIDIA GPU, remove `--gpus=all`)*.

3.  **Download the AI Model**:
    Once the container is running, execute this command to download the coding model:
    ```powershell
    docker exec -it ai-server ollama run qwen2.5-coder:1.5b
    ```
    *Wait for the `>>>` prompt to appear, then type `/bye` to exit.*

### Step 2: Build the Application
1.  Clone this repository or download the source code.
2.  Open the solution file (`.sln`) in **Visual Studio**.
3.  Right-click the Solution in Solution Explorer and select **Restore NuGet Packages**.
4.  Click **Build Solution** (Ctrl+Shift+B).

---

## 🎮 How to Use

1.  **Launch the App**: Click the green **Start** button in Visual Studio.
    * *The app will automatically try to wake up the Docker container if it's sleeping.*
2.  **Enter Repository URL**: Paste the GitHub link of the project you want to document (e.g., `https://github.com/username/project`).
3.  **Select Output Folder**: Choose where you want the PDF and Source Code to be saved.
4.  **Click "Generate Docs"**:
    * The app will clone the code.
    * It will analyze every file.
    * It will draw diagrams and schemas.
    * Finally, it will render the PDF.
5.  **View Results**: The PDF will automatically open when finished.

---

## 🐛 Troubleshooting

| Error | Solution |
| :--- | :--- |
| **"Protocol error (Performance.enable)"** | This usually happens if the PDF engine runs out of memory. The app handles this by disabling GPU acceleration for the browser. Restart the app and try again. |
| **"Docker container conflict"** | If the logs say the container name is in use, run `docker rm -f ai-server` in PowerShell and repeat Step 1. |
| **"Value cannot be an empty string"** | This has been patched in the latest version. Ensure you clean and rebuild the solution. |
| **Diagrams not showing** | Ensure you have a stable internet connection for the first run so `Mermaid.js` can load from the CDN. |

