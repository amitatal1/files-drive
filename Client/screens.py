import tkinter as tk
from tkinter import messagebox
import requests

API_URL = "http://127.0.0.1:123/api/"
AUTH_URL = API_URL + "auth/"  # Authentication endpoint
FILES_URL = API_URL + "files/"  # Files endpoint

class LoginScreen:
    def __init__(self, root, app):
        self.root = root
        self.app = app

    def show(self):
        self.clear_screen()
        self.root.configure(bg="#0A192F")  # Space background color

        tk.Label(self.root, text="🌌 Space Drive Login", font=("Arial", 18), fg="#64FFDA", bg="#0A192F").pack(pady=15)

        tk.Label(self.root, text="Username:", fg="white", bg="#0A192F").pack()
        self.username_entry = tk.Entry(self.root, bg="#112240", fg="white", insertbackground="white")
        self.username_entry.pack()

        tk.Label(self.root, text="Password:", fg="white", bg="#0A192F").pack()
        self.password_entry = tk.Entry(self.root, show="*", bg="#112240", fg="white", insertbackground="white")
        self.password_entry.pack()

        tk.Button(self.root, text="Login", command=self.handle_login, bg="#64FFDA", fg="black").pack(pady=10)
        tk.Button(self.root, text="Sign Up", command=self.handle_signup, bg="#112240", fg="white").pack()

    def handle_login(self):
        data = {"username": self.username_entry.get(), "password": self.password_entry.get()}
        try:
            response = requests.post(AUTH_URL + "login", json=data)
            if response.status_code == 200:
                token = response.json().get("token")
                if token:
                    self.app.user_token = token
                    self.app.show_home_screen()
                else:
                    messagebox.showerror("Login Failed", "Token not received!")
            else:
                error_message = response.json().get("errors", response.text)
                messagebox.showerror("Login Failed", f"Error: {error_message}")
                print(f"Error: {error_message}")
        except requests.exceptions.RequestException as e:
            messagebox.showerror("Request Error", f"An error occurred: {e}")

    def handle_signup(self):
        data = {"username": self.username_entry.get(), "password": self.password_entry.get()}
        try:
            response = requests.post(AUTH_URL + "signup", json=data)
            if response.status_code == 200:
                messagebox.showinfo("Sign Up", "Account created successfully!")
            else:
                error_message = response.json().get("errors", response.text)
                messagebox.showerror("Sign Up Failed", f"Error: {error_message}")
                print(f"Error: {error_message}")
        except requests.exceptions.RequestException as e:
            messagebox.showerror("Request Error", f"An error occurred: {e}")

    def clear_screen(self):
        for widget in self.root.winfo_children():
            widget.destroy()


class HomeScreen:
    def __init__(self, root, app):
        self.root = root
        self.app = app

    def show(self):
        self.clear_screen()
        self.root.configure(bg="#0A192F")

        tk.Label(self.root, text="🚀 Welcome to Space Drive 🚀", font=("Arial", 18), fg="#64FFDA", bg="#0A192F").pack(pady=10)

        tk.Button(self.root, text="Refresh Files", command=self.load_files, bg="#64FFDA", fg="black").pack(pady=5)
        self.file_list_frame = tk.Frame(self.root, bg="#0A192F")
        self.file_list_frame.pack(pady=10)

        self.load_files()

    def load_files(self):
        for widget in self.file_list_frame.winfo_children():
            widget.destroy()

        headers = {"Authorization": f"Bearer {self.app.user_token}"} if self.app.user_token else {}

        try:
            response = requests.get(FILES_URL + "listFiles", headers=headers)
            if response.status_code == 200:
                files = response.json()
                if files:
                    for file_info in files:
                        name = file_info.get("name", "Unknown")
                        data_type = file_info.get("data_type", "Unknown")
                        shared_with = ", ".join(file_info.get("shared_with", []))

                        tk.Label(self.file_list_frame, text=f"📁 {name} - {data_type} - Shared With: {shared_with}", fg="white", bg="#0A192F").pack(anchor="w")
                else:
                    tk.Label(self.file_list_frame, text="No files available", fg="#64FFDA", bg="#0A192F").pack()
            else:
                error_message = response.json().get("errors", response.text)
                messagebox.showerror("Error", f"Failed to load files: {error_message}")
                print(f"Error: {error_message}")

        except requests.exceptions.RequestException as e:
            messagebox.showerror("Error", f"Failed to fetch files: {e}")

    def clear_screen(self):
        for widget in self.root.winfo_children():
            widget.destroy()
