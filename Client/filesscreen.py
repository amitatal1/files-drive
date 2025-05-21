import tkinter as tk
from tkinter import messagebox, filedialog
import requests
import os
import webbrowser  # To open files with default application
import json # Import json for parsing potential error responses
import tempfile # Using tempfile for safer temporary file handling

# --- Define API URLs based on your server configuration ---
# Make sure these match the routes in your C# controller
# Assuming API_BASE_URL is defined elsewhere, e.g., in a config file or main app class
API_BASE_URL = "http://127.0.0.1:123/api/" # Base URL for your API
FILES_URL = f"{API_BASE_URL}files/"     # Base URL for file endpoints
# Specific endpoints:
UPLOAD_URL = f"{FILES_URL}upload" # Used for *new* uploads
EDIT_URL_TEMPLATE = f"{FILES_URL}edit/" # Append fileId: /api/files/edit/{fileId}
DOWNLOAD_URL_TEMPLATE = f"{FILES_URL}" # Append fileId: /api/files/{fileId}
SHARE_URL = f"{FILES_URL}share"
LIST_FILES_URL = f"{FILES_URL}listFiles" # Used by load_files, included for context
# -------------------------------------------------------


class HomeScreen:
    def __init__(self, root, app):
        self.root = root
        self.app = app # Assuming app object holds user_token and username
        self.file_list = []  # Store file data from listFiles endpoint

    def show(self):
        self.clear_screen()
        self.root.configure(bg="#0A192F")

        tk.Label(self.root, text="🚀 Welcome to Space Drive 🚀", font=("Arial", 18), fg="#64FFDA",
                 bg="#0A192F").pack(pady=10)

        tk.Button(self.root, text="Refresh Files", command=self.load_files, bg="#64FFDA", fg="black").pack(pady=5)
        tk.Button(self.root, text="Upload New File", command=self.open_upload_popup, bg="#64FFDA", fg="black").pack(pady=5) # Changed button text

        self.file_list_frame = tk.Frame(self.root, bg="#0A192F")
        self.file_list_frame.pack(pady=10, fill=tk.BOTH, expand=True) # Make frame expandable

        # Add a scrollbar if the list gets long
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
        popup.title("Upload New File") # Changed popup title
        popup.configure(bg="#0A192F")

        # Center the popup
        self.root.update_idletasks() # Update window size info
        x = self.root.winfo_x() + (self.root.winfo_width() // 2) - (popup.winfo_reqwidth() // 2)
        y = self.root.winfo_y() + (self.root.winfo_height() // 2) - (popup.winfo_reqheight() // 2)
        popup.geometry(f"+{x}+{y}")


        tk.Label(popup, text="Select a file to upload:", fg="white", bg="#0A192F").pack(pady=5)
        file_path_entry = tk.Entry(popup, width=40)
        file_path_entry.pack(padx=10) 

        def browse_file():
            file_path = filedialog.askopenfilename()
            if file_path: # Only update if a file was selected
                file_path_entry.delete(0, tk.END)
                file_path_entry.insert(0, file_path)

        tk.Button(popup, text="Browse", command=browse_file, bg="#64FFDA", fg="black").pack(pady=5)

        # Removed the "Enter users to share with" section as initial sharing is no longer
        # handled by the upload endpoint in the refactored backend.
        # Sharing is now a separate action on the file details page.

        def upload_file():
            file_path = file_path_entry.get()
            if not file_path or not os.path.exists(file_path): # Added check for file existence
                messagebox.showerror("Error", "Please select a valid file.")
                return

            # Call the dedicated upload function (no more sharing logic here)
            if self.upload_new_file_to_api(file_path):
                popup.destroy()
                self.load_files()  # Reload the file list after successful upload

        tk.Button(popup, text="Upload", command=upload_file, bg="#64FFDA", fg="black").pack(pady=5)

    def load_files(self):
        # Destroy widgets in the scrollable frame
        for widget in self.scrollable_frame.winfo_children():
            widget.destroy()

        headers = {"Authorization": f"Bearer {self.app.user_token}"} if self.app.user_token else {}

        try:
            # Use the correct listFiles URL
            response = requests.get(LIST_FILES_URL, headers=headers)

            # Use the new centralized error handler
            if self._handle_api_response(response, "Failed to load files"):
                self.file_list = response.json()
                if self.file_list:
                    for file_info in self.file_list:
                        # Extract relevant info from the backend response structure
                        file_id = file_info.get("id") # Backend sends "id"
                        name = file_info.get("fileName", "Unknown") # Backend sends "fileName"
                        data_type = name.split(".")[-1].upper() if "." in name else "Unknown" # Use upper for type
                        file_owner = file_info.get("owner", "Unknown") # Backend sends "owner"
                        # Backend sends lists for permissions, but we don't display them directly here
                        # file_info.get("viewPermissions", [])
                        # file_info.get("editPermissions", [])

                        file_label = tk.Label(self.scrollable_frame, text=f"📁 {name} - Type: {data_type} - Owner: {file_owner}",
                                             fg="white", bg="#0A192F", cursor="hand2") # Added cursor
                        file_label.pack(anchor="w", fill=tk.X, padx=5, pady=2) # Added padding, fill
                        # Bind click event, passing file_id and filename
                        file_label.bind("<Button-1>",
                                         lambda event, fid=file_id, fname=name: self.open_file_details(fid, fname))

                    # Update the scrollable region after adding files
                    self.scrollable_frame.update_idletasks()
                    self.canvas.config(scrollregion=self.canvas.bbox("all"))

                else:
                    tk.Label(self.scrollable_frame, text="No files available", fg="#64FFDA",
                             bg="#0A192F").pack(pady=10)
        except requests.RequestException as e:
            messagebox.showerror("Error", f"Network error fetching files: {e}")
        except Exception as e: # Catch potential JSON parsing errors or other unexpected issues
            messagebox.showerror("Error", f"An unexpected error occurred: {e}")


    def clear_screen(self):
        for widget in self.root.winfo_children():
            widget.destroy()

    def open_file_details(self, file_id, filename):
        """Opens a popup window to display file details and actions."""
        # Find the file_info object using the file_id
        file_info = next((f for f in self.file_list if f.get("id") == file_id), None)

        if not file_info:
            messagebox.showerror("Error", "File details not found in the list.")
            return # Exit if file_info is not found


        file_details_window = tk.Toplevel(self.root)
        file_details_window.title(f"File Details - {filename}")
        file_details_window.configure(bg="#0A192F")

        # Center the popup relative to the root window
        self.root.update_idletasks() # Update window size info
        x = self.root.winfo_x() + (self.root.winfo_width() // 2) - (file_details_window.winfo_reqwidth() // 2)
        y = self.root.winfo_y() + (self.root.winfo_height() // 2) - (file_details_window.winfo_reqheight() // 2)
        file_details_window.geometry(f"+{x}+{y}")


        tk.Label(file_details_window, text=f"File Name: {file_info.get('fileName', 'N/A')}", fg="white",
                     bg="#0A192F").pack(pady=5, padx=10, anchor="w") # Added padding, anchor
        tk.Label(file_details_window, text=f"Owner: {file_info.get('owner', 'N/A')}", fg="white",
                     bg="#0A192F").pack(pady=5, padx=10, anchor="w") # Display owner
        tk.Label(file_details_window, text=f"Last Updated: {file_info.get('lastUpdateTime', 'N/A')}", fg="white",
                     bg="#0A192F").pack(pady=5, padx=10, anchor="w") # Use correct field name


        # Add buttons for file actions
        # Edit button: Only show if user has edit permissions or is owner
        current_user = self.app.username # Assuming username is stored in app
        is_owner = current_user == file_info.get('owner')
        has_edit_permission = current_user in file_info.get('editPermissions', [])

        if is_owner or has_edit_permission:
            tk.Button(file_details_window, text="Upload New Version (Edit)", # Changed button text
                      command=lambda: self.upload_new_version(file_id, file_details_window), # Pass only file_id and parent
                      bg="#64FFDA", fg="black").pack(pady=5)

        # Share button: Only show if user is the owner
        if is_owner: # Check if current user is the owner
            tk.Button(file_details_window, text="Edit Permissions (Share)", # Changed button text
                      command=lambda: self.edit_permissions(
                            file_id,
                            file_info.get('viewPermissions', []), # Use correct field names
                            file_info.get('editPermissions', []), # Use correct field names
                            file_details_window
                        ),
                        bg="#64FFDA", fg="black").pack(pady=5)

        # Download button: Show if user has view, edit, or owner permissions
        has_view_permission = current_user in file_info.get('viewPermissions', [])
        if is_owner or has_edit_permission or has_view_permission:
            tk.Button(file_details_window, text="Download",
                      command=lambda: self.download_file_from_api(file_id, filename), # Pass file_id and filename
                      bg="#64FFDA", fg="black").pack(pady=5)
            tk.Button(file_details_window, text="Preview",
                      command=lambda: self.preview_file(file_id, filename), # Pass file_id and filename
                      bg="#64FFDA", fg="black").pack(pady=5)
        else:
            tk.Label(file_details_window, text="You do not have permissions to view this file.", fg="red", bg="#0A192F").pack(pady=5)


    def _handle_api_response(self, response: requests.Response, context_message: str) -> bool:
        """
        Handles common API response status codes and displays appropriate messages.

        Args:
            response (requests.Response): The response object from the API call.
            context_message (str): A brief message describing the action that failed
                                   (e.g., "Upload Failed", "Download Failed").

        Returns:
            bool: True if the response indicates success (status 200 or 201), False otherwise.
                  If False, an error message box is displayed or the user is redirected.
        """
        if response.status_code in (200, 201):
            return True # Success

        try:
            # Attempt to get a specific error message from the JSON response
            error_data = response.json()
            message = error_data.get("Message", f"API Error: Status {response.status_code}")
        except json.JSONDecodeError:
            # If the response is not JSON, use the raw text
            message = f"API Error: Status {response.status_code}\nResponse: {response.text[:200]}..." # Show first 200 chars

        if response.status_code == 401:
            messagebox.showerror("Authentication Error", f" {context_message} Your session has expired. Please log in again. {message}", )
            self.app.show_login_screen() # Redirect to login screen
        elif response.status_code == 403:
            messagebox.showerror(f"{context_message} - Permission Denied", message)
        elif response.status_code == 404:
            messagebox.showerror(f"{context_message} - Not Found", message)
        elif response.status_code == 409: # Conflict (e.g., file name already exists)
             messagebox.showerror(f"{context_message} - Conflict", message)
        elif response.status_code >= 400 and response.status_code < 500:
            # Client errors (4xx) other than those handled specifically
            messagebox.showerror(f"{context_message} - Client Error", message)
        elif response.status_code >= 500:
            # Server errors (5xx)
            messagebox.showerror(f"{context_message} - Server Error", message)
        else:
            # Any other unhandled status codes
            messagebox.showerror(f"{context_message} - Unexpected Status", message)

        return False # Indicates failure

    # --- Helper functions called by the action buttons (now using the error handler) ---

    def upload_new_file_to_api(self, file_path):
        """Uploads a *new* file to the backend API."""
        headers = {"Authorization": f"Bearer {self.app.user_token}"}
        filename = os.path.basename(file_path)

        try:
            with open(file_path, "rb") as f:
                files = {"file": (filename, f)} # 'file' should match the parameter name in backend controller action (IFormFile file)
                # The backend /upload endpoint doesn't need owner or fileId params anymore
                resp = requests.post(UPLOAD_URL, headers=headers, files=files)

            # Use the centralized error handler
            if self._handle_api_response(resp, "Upload Failed"):
                messagebox.showinfo("Success", "File uploaded successfully.")
                return True
            else:
                return False # Error handled by _handle_api_response

        except requests.RequestException as e:
            messagebox.showerror("Error", f"Network error during upload: {e}")
            return False

    def edit_file_on_api(self, file_id, file_path):
        """Uploads a new version of an existing file (edits) via the API.

        Args:
            file_id (str): The ObjectId string of the file to edit.
            file_path (str): Full path to the local file with the new content.

        Returns:
            bool: True if the edit was successful, False otherwise.
        """
        headers = {"Authorization": f"Bearer {self.app.user_token}"}
        filename = os.path.basename(file_path) # Get filename from the new file path

        try:
            with open(file_path, "rb") as f:
                files = {"file": (filename, f)} # 'file' should match the parameter name in backend controller action (IFormFile file)
                # Use PUT request to the specific edit URL with fileId in the path
                resp = requests.put(f"{EDIT_URL_TEMPLATE}{file_id}", headers=headers, files=files)

            # Use the centralized error handler
            if self._handle_api_response(resp, "Edit Failed"):
                 messagebox.showinfo("Success", "File edited successfully.")
                 return True
            else:
                 return False # Error handled by _handle_api_response


        except requests.RequestException as e:
            messagebox.showerror("Error", f"Network error during edit: {e}")
            return False


    def update_permissions_api(self, file_id, view_users, edit_users):
        """Updates file permissions via the API using file ID.

        Args:
            file_id (str): The ObjectId string of the file to update permissions for.
            view_users (list): List of usernames with view permissions.
            edit_users (list): List of usernames with edit permissions.

        Returns:
            bool: True if permissions were updated successfully, False otherwise.
        """
        headers = {"Authorization": f"Bearer {self.app.user_token}"}

        # Backend expects fileId, editPermissions, viewPermissions as QUERY parameters
        params = {
            "fileId": file_id,
            "viewPermissions": view_users,  # Correct parameter name
            "editPermissions": edit_users  # Correct parameter name
        }
        # requests library handles lists in params correctly by repeating the key:
        # ?fileId=...&viewPermissions=user1&viewPermissions=user2&editPermissions=user1 etc.


        try:
            # Use POST request to the SHARE_URL, passing params
            response = requests.post(SHARE_URL, headers=headers, params=params)

            # Use the centralized error handler
            if self._handle_api_response(response, "Update Permissions Failed"):
                messagebox.showinfo("Success", "Permissions updated successfully!")
                return True
            else:
                return False # Error handled by _handle_api_response


        except requests.RequestException as e:
            messagebox.showerror("Error", f"Network error updating permissions: {e}")
            return False


    def download_file_from_api(self, file_id, filename): # Keep filename to suggest local save name
        """Downloads a file from the Space Drive API using file ID.

        Args:
            file_id (str): The ObjectId string of the file to download.
            filename (str): The original filename (used to suggest local save name).
        """
        headers = {"Authorization": f"Bearer {self.app.user_token}"}
        try:
            # Use GET request to the download URL template with fileId in the path
            response = requests.get(f"{DOWNLOAD_URL_TEMPLATE}{file_id}", headers=headers,
                                     stream=True)  # Use stream=True for large files

            # Use the centralized error handler first
            if self._handle_api_response(response, "Download Failed"):
                # If successful, proceed with saving the file
                # Use a file dialog to choose the download location
                save_path = filedialog.asksaveasfilename(
                    initialfile=filename,  # Suggest the original filename
                    defaultextension=".*",  # Allow any file extension
                    title="Save File"
                )
                if save_path:  # Only save if a path was selected
                    try:
                        with open(save_path, 'wb') as f:
                            for chunk in response.iter_content(chunk_size=8192):
                                # If you have connection issues during download, you might get HTML error pages as chunks.
                                # A more robust client might check headers or content type.
                                f.write(chunk)
                        messagebox.showinfo("Download", "File downloaded successfully!")
                    except Exception as save_error:
                        messagebox.showerror("Save Error", f"Failed to save file locally: {save_error}")
                else: # User cancelled save dialog
                    messagebox.showinfo("Download Cancelled", "File download was cancelled.")
            # If _handle_api_response returned False, it already displayed an error message.


        except requests.RequestException as e:
            messagebox.showerror("Error", f"Network error during download: {e}")


    def preview_file(self, file_id, filename): # Added file_id parameter
        """Downloads the file to a temporary location and opens it with the default application.

        Args:
            file_id (str): The ObjectId string of the file to preview.
            filename (str): The original filename (used for the temporary file name).
        """
        headers = {"Authorization": f"Bearer {self.app.user_token}"}
        try:
            # Use GET request to the download URL template with fileId in the path
            response = requests.get(f"{DOWNLOAD_URL_TEMPLATE}{file_id}", headers=headers, stream=True) # Use file_id

            # Use the centralized error handler first
            if self._handle_api_response(response, "Preview Failed"):
                # If successful, proceed with saving and opening the file
                # Save the file to a temporary location using tempfile
                try:
                    # Create a temporary file with the original filename and extension
                    # It will be automatically deleted when closed or the program exits
                    with tempfile.NamedTemporaryFile(prefix="spacedrive_preview_", suffix=os.path.splitext(filename)[1], delete=False) as tmp_file:
                        temp_file_path = tmp_file.name # Get the actual path of the temporary file
                        for chunk in response.iter_content(chunk_size=8192):
                            tmp_file.write(chunk)

                    # Open the file with the default application
                    # Consider cross-platform compatibility (os.startfile on Windows, open on macOS, xdg-open on Linux)
                    # webbrowser.open is a good cross-platform starting point
                    webbrowser.open(temp_file_path)

                    # Note: The tempfile module with delete=False requires manual cleanup
                    # os.remove(temp_file_path) # You might want to add logic to clean this up later
                    # For simplicity here, we leave it, but in a real app, manage temp files.

                except Exception as save_open_error:
                    messagebox.showerror("Preview Failed", f"Failed to save or open temporary file: {save_open_error}")
                    # Attempt to clean up the temp file if it was created but opening failed
                    if 'temp_file_path' in locals() and os.path.exists(temp_file_path):
                        try: os.remove(temp_file_path)
                        except: pass # Ignore errors during cleanup

            # If _handle_api_response returned False, it already displayed an error message.


        except requests.RequestException as e:
            messagebox.showerror("Error", f"Network error during preview: {e}")
        except Exception as ex:
            messagebox.showerror("Error", f"An unexpected error occurred during preview: {ex}")


    # --- Functions called by the buttons in the file details popup ---

    def upload_new_version(self, file_id, parent_window):
        """Handles the 'Upload New Version' button click."""
        file_path = filedialog.askopenfilename(title="Select New Version")
        if file_path:
            # Call the dedicated edit function
            if self.edit_file_on_api(file_id, file_path):
                # On successful edit, reload the file list and close the details window
                self.load_files()
                parent_window.destroy()
            # Error handling is done inside edit_file_on_api


    def edit_permissions(self, file_id, initial_view_users, initial_edit_users, parent_window):
        """Handles the 'Edit Permissions' button click, opens a popup."""
        permissions_window = tk.Toplevel(self.root)
        permissions_window.title("Edit Permissions")
        permissions_window.configure(bg="#0A192F")

        # Center the popup
        self.root.update_idletasks()
        x = self.root.winfo_x() + (self.root.winfo_width() // 2) - (permissions_window.winfo_reqwidth() // 2)
        y = self.root.winfo_y() + (self.root.winfo_height() // 2) - (permissions_window.winfo_reqheight() // 2)
        permissions_window.geometry(f"+{x}+{y}")

        tk.Label(permissions_window, text="View Permissions (comma-separated usernames):", fg="white",
                 bg="#0A192F").pack(pady=5, padx=10, anchor="w")
        view_users_entry = tk.Entry(permissions_window, width=40)
        view_users_entry.insert(0, ",".join(initial_view_users))
        view_users_entry.pack(padx=10)

        tk.Label(permissions_window, text="Edit Permissions (comma-separated usernames):", fg="white",
                 bg="#0A192F").pack(pady=5, padx=10, anchor="w")
        edit_users_entry = tk.Entry(permissions_window, width=40)
        edit_users_entry.insert(0, ",".join(initial_edit_users))
        edit_users_entry.pack(padx=10)

        def save_permissions():
            # Split and strip whitespace from usernames
            view_users = [u.strip() for u in view_users_entry.get().split(",") if u.strip()]
            edit_users = [u.strip() for u in edit_users_entry.get().split(",") if u.strip()]

            # Optional: Add owner back if they were somehow removed from the lists (backend already does this)
            # owner = next((f.get('owner') for f in self.file_list if f.get('id') == file_id), None)
            # if owner and owner not in view_users: view_users.append(owner)
            # if owner and owner not in edit_users: edit_users.append(owner)

            if self.update_permissions_api(file_id, view_users, edit_users):
                permissions_window.destroy()
                # Reload and close parent window after successful update
                self.load_files()
                parent_window.destroy()
            # Error handling is inside update_permissions_api


        tk.Button(permissions_window, text="Save Permissions", command=save_permissions, bg="#64FFDA",
                 fg="black").pack(pady=10)
