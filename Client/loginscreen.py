import tkinter as tk
from tkinter import messagebox
import requests

API_URL = "http://127.0.0.1:123/api/"
AUTH_URL = API_URL + "auth/"  # Authentication endpoint

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
                    self.app.username = self.username_entry.get()
                    self.app.show_home_screen()
                else:
                    messagebox.showerror("Login Failed", "Token not received!")
            else:
                error_message = response.text
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
                error_message = response.text
                messagebox.showerror("Sign Up Failed", f"Error: {error_message}")
                print(f"Error: {error_message}")
        except requests.exceptions.RequestException as e:
            messagebox.showerror("Request Error", f"An error occurred: {e}")

    def clear_screen(self):
        for widget in self.root.winfo_children():
            widget.destroy()
