import tkinter as tk
from filesscreen import HomeScreen
from loginscreen import LoginScreen
class FileSharingApp:
    def __init__(self, root):
        self.root = root
        self.root.geometry("500x400")
        self.root.title("Space Drive")
        self.root.configure(bg="#0A192F")  # Dark background for space theme

        self.user_token = None  
        self.username = None
        # Initialize screens
        self.login_screen = LoginScreen(root, self)
        self.home_screen = HomeScreen(root, self)

        # Show the login screen initially
        self.show_login_screen()

    def show_login_screen(self):
        self.login_screen.show()

    def show_home_screen(self):
        self.home_screen.show()

if __name__ == "__main__":
    root = tk.Tk()
    app = FileSharingApp(root)
    root.mainloop()
