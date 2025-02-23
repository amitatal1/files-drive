import tkinter as tk
from tkinter import messagebox
import requests

API_URL = "http://127.0.0.1:8000/files"  # REST API endpoint


class LoginScreen:
    def __init__(self, root, app):
        self.root = root
        self.app = app

    def show(self):
        self.clear_screen()
        
        tk.Label(self.root, text="Login / Sign-Up", font=("Arial", 16)).pack(pady=20)

        tk.Label(self.root, text="Username:").pack()
        self.username_entry = tk.Entry(self.root)
        self.username_entry.pack()

        tk.Label(self.root, text="Password:").pack()
        self.password_entry = tk.Entry(self.root, show="*")
        self.password_entry.pack()

        tk.Button(self.root, text="Login", command=self.handle_login).pack(pady=5)
        tk.Button(self.root, text="Sign Up", command=self.handle_signup).pack()

    def handle_login(self):
        username = self.username_entry.get()
        if username == "test":  # Dummy check
            self.app.home_screen.show()
        else:
            messagebox.showerror("Login Failed", "Invalid username or password")

    def handle_signup(self):
        username = self.username_entry.get()
        if username:
            messagebox.showinfo("Sign Up", "Account created successfully!")
        else:
            messagebox.showerror("Error", "Please enter a username")

    def clear_screen(self):
        for widget in self.root.winfo_children():
            widget.destroy()


class HomeScreen:
    def __init__(self, root):
        self.root = root

    def show(self):
        self.clear_screen()
        tk.Label(self.root, text="File Sharing Home", font=("Arial", 16)).pack(pady=10)
        tk.Button(self.root, text="Refresh File List", command=self.load_files).pack(pady=5)
        self.file_list_frame = tk.Frame(self.root)
        self.file_list_frame.pack(pady=10)

    def load_files(self):
        # Clear previous file list
        for widget in self.file_list_frame.winfo_children():
            widget.destroy()

        try:
            response = requests.get(API_URL)
            files = response.json()

            for file_info in files:
                name = file_info['name']
                data_type = file_info['data_type']
                shared_with = ", ".join(file_info['shared_with'])

                tk.Label(self.file_list_frame, text=f"Name: {name}, Type: {data_type}, Shared With: {shared_with}") \
                    .pack(anchor="w")
        except Exception as e:
            messagebox.showerror("Error", f"Failed to fetch files: {e}")
    
    def clear_screen(self):
        for widget in self.root.winfo_children():
            widget.destroy()
