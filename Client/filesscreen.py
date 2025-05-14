import tkinter as tk
from tkinter import messagebox, filedialog
import requests
import os
import webbrowser  # To open files with default application
import json  # Import json for parsing potential error responses

# --- Define API URLs based on your server configuration ---
API_BASE_URL = "http://127.0.0.1:123/api/"
FILES_URL = f"{API_BASE_URL}files/"
UPLOAD_URL = f"{FILES_URL}upload"
EDIT_URL_TEMPLATE = f"{FILES_URL}edit/"  # Append fileId
DOWNLOAD_URL_TEMPLATE = f"{FILES_URL}"   # Append fileId
SHARE_URL = f"{FILES_URL}share"
LIST_FILES_URL = f"{FILES_URL}listFiles"
# -------------------------------------------------------

class HomeScreen:
    def __init__(self, root, app):
        self.root = root
        self.app = app  # Assuming app object holds user_token and username
        self.file_list = []  # Store file data from listFiles endpoint

    def show(self):
        self.clear_screen()
        self.root.configure(bg="#0A192F")

        tk.Label(self.root, text="🚀 Welcome to Space Drive 🚀", font=("Arial", 18), fg="#64FFDA",
                 bg="#0A192F").pack(pady=10)

        tk.Button(self.root, text="Refresh Files", command=self.load_files, bg="#64FFDA", fg="black").pack(pady=5)
        tk.Button(self.root, text="Upload New File", command=self.open_upload_popup, bg="#64FFDA", fg="black").pack(pady=5)

        self.file_list_frame = tk.Frame(self.root, bg="#0A192F")
        self.file_list_frame.pack(pady=10, fill=tk.BOTH, expand=True)

        self.canvas = tk.Canvas(self.file_list_frame, bg="#0A192F")
        self.scrollbar = tk.Scrollbar(self.file_list_frame, orient="vertical", command=self.canvas.yview)
        self.scrollable_frame = tk.Frame(self.canvas, bg="#0A192F")

        self.scrollable_frame.bind(
            "<Configure>",
            lambda e: self.canvas.configure(
                scrollregion=self.canvas.bbox("all")
            )
        )

        self.canvas.create_window((0, 0), window=self.scrollable_frame, anchor="nw")
        self.canvas.configure(yscrollcommand=self.scrollbar.set)

        self.canvas.pack(side="left", fill="both", expand=True)
        self.scrollbar.pack(side="right", fill="y")

        self.load_files()

    def open_upload_popup(self):
        popup = tk.Toplevel(self.root)
        popup.title("Upload New File")
        popup.configure(bg="#0A192F")

        self.root.update_idletasks()
        x = self.root.winfo_x() + (self.root.winfo_width() // 2) - (popup.winfo_reqwidth() // 2)
        y = self.root.winfo_y() + (self.root.winfo_height() // 2) - (popup.winfo_reqheight() // 2)
        popup.geometry(f"+{x}+{y}")

        tk.Label(popup, text="Select a file to upload:", fg="white", bg="#0A192F").pack(pady=5)
        file_path_entry = tk.Entry(popup, width=40)
        file_path_entry.pack(padx=10)

        def browse_file():
            file_path = filedialog.askopenfilename()
            if file_path:
                file_path_entry.delete(0, tk.END)
                file_path_entry.insert(0, file_path)

        tk.Button(popup, text="Browse", command=browse_file, bg="#64FFDA", fg="black").pack(pady=5)

        def upload_file():
            file_path = file_path_entry.get()
            if not file_path or not os.path.exists(file_path):
                messagebox.showerror("Error", "Please select a valid file.")
                return
            if self.upload_new_file_to_api(file_path):
                popup.destroy()
                self.load_files()

        tk.Button(popup, text="Upload", command=upload_file, bg="#64FFDA", fg="black").pack(pady=5)

    def load_files(self):
        for widget in self.scrollable_frame.winfo_children():
            widget.destroy()

        headers = {"Authorization": f"Bearer {self.app.user_token}"} if self.app.user_token else {}

        try:
            response = requests.get(LIST_FILES_URL, headers=headers)
            if response.status_code == 200:
                self.file_list = response.json()
                if self.file_list:
                    for file_info in self.file_list:
                        file_id = file_info.get("id")
                        name = file_info.get("fileName", "Unknown")
                        data_type = name.split(".")[-1].upper() if "." in name else "Unknown"
                        file_owner = file_info.get("owner", "Unknown")

                        file_label = tk.Label(self.scrollable_frame, text=f"📁 {name} - Type: {data_type} - Owner: {file_owner}",
                                              fg="white", bg="#0A192F", cursor="hand2")
                        file_label.pack(anchor="w", fill=tk.X, padx=5, pady=2)
                        file_label.bind("<Button-1>",
                                        lambda event, fid=file_id, fname=name: self.open_file_details(fid, fname))

                    self.scrollable_frame.update_idletasks()
                    self.canvas.config(scrollregion=self.canvas.bbox("all"))
                else:
                    tk.Label(self.scrollable_frame, text="No files available", fg="#64FFDA",
                             bg="#0A192F").pack(pady=10)
            elif response.status_code == 401:
                messagebox.showerror("Authentication Error", "Your session has expired. Please log in again.")
                self.app.show_login_screen()
            else:
                messagebox.showerror("Error", f"Failed to load files. Status: {response.status_code}")
        except requests.RequestException as e:
            messagebox.showerror("Error", f"Network error fetching files: {e}")
        except Exception as e:
            messagebox.showerror("Error", f"An unexpected error occurred: {e}")

    def clear_screen(self):
        for widget in self.root.winfo_children():
            widget.destroy()

    def open_file_details(self, file_id, filename):
        file_info = next((f for f in self.file_list if f.get("id") == file_id), None)
        if not file_info:
            messagebox.showerror("Error", "File details not found in the list.")
            return

        file_details_window = tk.Toplevel(self.root)
        file_details_window.title(f"File Details - {filename}")
        file_details_window.configure(bg="#0A192F")

        self.root.update_idletasks()
        x = self.root.winfo_x() + (self.root.winfo_width() // 2) - (file_details_window.winfo_reqwidth() // 2)
        y = self.root.winfo_y() + (self.root.winfo_height() // 2) - (file_details_window.winfo_reqheight() // 2)
        file_details_window.geometry(f"+{x}+{y}")

        tk.Label(file_details_window, text=f"File Name: {file_info.get('fileName', 'N/A')}", fg="white",
                 bg="#0A192F").pack(pady=5, padx=10, anchor="w")
        tk.Label(file_details_window, text=f"Owner: {file_info.get('owner', 'N/A')}", fg="white",
                 bg="#0A192F").pack(pady=5, padx=10, anchor="w")
        tk.Label(file_details_window, text=f"Last Updated: {file_info.get('lastUpdateTime', 'N/A')}", fg="white",
                 bg="#0A192F").pack(pady=5, padx=10, anchor="w")

        current_user = self.app.username
        is_owner = current_user == file_info.get('owner')
        has_edit_permission = current_user in file_info.get('editPermissions', [])

        if is_owner or has_edit_permission:
            tk.Button(file_details_window, text="Upload New Version (Edit)",
                      command=lambda: self.upload_new_version(file_id, file_details_window),
                      bg="#64FFDA", fg="black").pack(pady=5)

        if is_owner:
            tk.Button(file_details_window, text="Edit Permissions (Share)",
                      command=lambda: self.edit_permissions(
                          file_id,
                          file_info.get('viewPermissions', []),
                          file_info.get('editPermissions', []),
                          file_details_window
                      ),
                      bg="#64FFDA", fg="black").pack(pady=5)

        has_view_permission = current_user in file_info.get('viewPermissions', [])
        if is_owner or has_edit_permission or has_view_permission:
            tk.Button(file_details_window, text="Download",
                      command=lambda: self.download_file_from_api(file_id, filename),
                      bg="#64FFDA", fg="black").pack(pady=5)
            tk.Button(file_details_window, text="Preview",
                      command=lambda: self.preview_file(file_id, filename),
                      bg="#64FFDA", fg="black").pack(pady=5)
        else:
            tk.Label(file_details_window, text="You do not have permissions to view this file.",
                     fg="red", bg="#0A192F").pack(pady=5)
