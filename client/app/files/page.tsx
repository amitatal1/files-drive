"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import { getFiles } from "../../utils/api"
import Link from "next/link"

interface File {
  id: string
  name: string
  isOwned: boolean
}

export default function Files() {
  const [files, setFiles] = useState<File[]>([])
  const router = useRouter()

  useEffect(() => {
    const fetchFiles = async () => {
      try {
        const data = await getFiles()
        setFiles(data.files)
      } catch (error) {
        console.error("Failed to fetch files:", error)
        router.push("/login")
      }
    }
    fetchFiles()
  }, [router])

  return (
    <div>
      <h1 className="text-2xl font-bold mb-5">Your Files</h1>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {files.map((file) => (
          <Link href={`/files/${file.id}`} key={file.id}>
            <div className="border p-4 rounded cursor-pointer hover:bg-gray-100">
              <h2 className="font-semibold">{file.name}</h2>
              <p>{file.isOwned ? "Owned" : "Shared with you"}</p>
            </div>
          </Link>
        ))}
      </div>
    </div>
  )
}

