import tkinter as tk
from screens import LoginScreen, HomeScreen


class FileSharingApp:
    def __init__(self, root):
        self.root = root
        self.root.geometry("400x300")

        # Initialize screens
        self.home_screen = HomeScreen(root)
        self.login_screen = LoginScreen(root, self)

        # Show the login screen initially
        self.login_screen.show()


if __name__ == "__main__":
    root = tk.Tk()
    app = FileSharingApp(root)
    root.mainloop()
