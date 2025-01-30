const API_URL = "http://localhost:5000"

export async function login(email: string, password: string) {
  const response = await fetch(`${API_URL}/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  })
  return response.json()
}

export async function signup(email: string, password: string) {
  const response = await fetch(`${API_URL}/signup`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  })
  return response.json()
}

export async function getFiles() {
  const response = await fetch(`${API_URL}/files`, {
    headers: { Authorization: `Bearer ${localStorage.getItem("token")}` },
  })
  return response.json()
}

export async function getFileDetails(fileId: string) {
  const response = await fetch(`${API_URL}/files/${fileId}`, {
    headers: { Authorization: `Bearer ${localStorage.getItem("token")}` },
  })
  return response.json()
}

export async function shareFile(fileId: string, email: string) {
  const response = await fetch(`${API_URL}/files/${fileId}/share`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${localStorage.getItem("token")}`,
    },
    body: JSON.stringify({ email }),
  })
  return response.json()
}

export async function updateFile(fileId: string, content: string) {
  const response = await fetch(`${API_URL}/files/${fileId}`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${localStorage.getItem("token")}`,
    },
    body: JSON.stringify({ content }),
  })
  return response.json()
}

