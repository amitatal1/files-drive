"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import { getFileDetails, shareFile, updateFile } from "../../../utils/api"

interface FileDetails {
  id: string
  name: string
  content: string
  isOwned: boolean
}

export default function FileDetails({ params }: { params: { id: string } }) {
  const [file, setFile] = useState<FileDetails | null>(null)
  const [content, setContent] = useState("")
  const [shareEmail, setShareEmail] = useState("")
  const router = useRouter()

  useEffect(() => {
    const fetchFileDetails = async () => {
      try {
        const data = await getFileDetails(params.id)
        setFile(data.file)
        setContent(data.file.content)
      } catch (error) {
        console.error("Failed to fetch file details:", error)
        router.push("/files")
      }
    }
    fetchFileDetails()
  }, [params.id, router])

  const handleShare = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      await shareFile(params.id, shareEmail)
      alert("File shared successfully")
      setShareEmail("")
    } catch (error) {
      console.error("Failed to share file:", error)
    }
  }

  const handleUpdate = async () => {
    try {
      await updateFile(params.id, content)
      alert("File updated successfully")
    } catch (error) {
      console.error("Failed to update file:", error)
    }
  }

  if (!file) return <div>Loading...</div>

  return (
    <div>
      <h1 className="text-2xl font-bold mb-5">{file.name}</h1>
      <textarea
        value={content}
        onChange={(e) => setContent(e.target.value)}
        className="w-full h-64 p-2 border rounded mb-4"
      />
      <button onClick={handleUpdate} className="bg-blue-500 text-white px-4 py-2 rounded mr-2">
        Update Content
      </button>
      {file.isOwned && (
        <form onSubmit={handleShare} className="mt-4">
          <input
            type="email"
            value={shareEmail}
            onChange={(e) => setShareEmail(e.target.value)}
            placeholder="Enter email to share"
            className="px-3 py-2 border rounded mr-2"
          />
          <button type="submit" className="bg-green-500 text-white px-4 py-2 rounded">
            Share
          </button>
        </form>
      )}
    </div>
  )
}

