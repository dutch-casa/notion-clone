import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';

interface UploadImageParams {
  file: File;
  pageId: string;
  orgId: string;
}

interface UploadImageResult {
  id: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  fileUrl: string;
  pageId: string;
  orgId: string;
  uploadedBy: string;
  uploadedAt: string;
}

export function useImageUpload() {
  return useMutation({
    mutationFn: async ({ file, pageId, orgId }: UploadImageParams): Promise<UploadImageResult> => {
      const formData = new FormData();
      formData.append('File', file);
      formData.append('PageId', pageId);
      formData.append('OrgId', orgId);

      const response = await fetch(`${import.meta.env.VITE_API_URL || 'http://localhost:5036'}/api/Images/upload`, {
        method: 'POST',
        credentials: 'include',
        body: formData,
      });

      if (!response.ok) {
        const error = await response.json().catch(() => ({ detail: 'Failed to upload image' }));
        throw new Error(error.detail || 'Failed to upload image');
      }

      return response.json();
    },
    onError: (error: Error) => {
      toast.error(error.message || 'Failed to upload image');
    },
    onSuccess: () => {
      toast.success('Image uploaded successfully');
    },
  });
}

export function useImageDelete() {
  return useMutation({
    mutationFn: async (imageId: string): Promise<void> => {
      const response = await fetch(`${import.meta.env.VITE_API_URL || 'http://localhost:5036'}/api/Images/${imageId}`, {
        method: 'DELETE',
        credentials: 'include',
      });

      if (!response.ok) {
        const error = await response.json().catch(() => ({ detail: 'Failed to delete image' }));
        throw new Error(error.detail || 'Failed to delete image');
      }
    },
    onError: (error: Error) => {
      toast.error(error.message || 'Failed to delete image');
    },
    onSuccess: () => {
      toast.success('Image deleted successfully');
    },
  });
}
