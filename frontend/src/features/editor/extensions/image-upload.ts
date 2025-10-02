import { Editor, Extension } from '@tiptap/core';
import { Plugin, PluginKey } from '@tiptap/pm/state';

interface ImageUploadOptions {
  onUpload: (file: File) => Promise<string>;
  onDelete?: (imageUrl: string) => Promise<void>;
}

/**
 * Custom Tiptap extension to handle image upload via drag-drop and paste.
 * Uses editorProps to intercept drop and paste events.
 */
export const ImageUpload = Extension.create<ImageUploadOptions>({
  name: 'imageUpload',

  addOptions() {
    return {
      onUpload: () => {
        throw new Error('onUpload handler is required');
      },
      onDelete: undefined,
    };
  },

  addProseMirrorPlugins() {
    const { onUpload } = this.options;
    const editor = this.editor;

    return [
      new Plugin({
        key: new PluginKey('imageUpload'),
        props: {
          handleDrop(view, event, _slice, moved) {
            if (moved) return false;

            const files = Array.from(event.dataTransfer?.files || []);
            const imageFiles = files.filter(file => file.type.startsWith('image/'));

            if (imageFiles.length === 0) return false;

            event.preventDefault();

            const { schema } = view.state;
            const coordinates = view.posAtCoords({
              left: event.clientX,
              top: event.clientY,
            });

            imageFiles.forEach(async (file) => {
              // Show loading placeholder
              const placeholderSrc = URL.createObjectURL(file);

              if (coordinates) {
                const node = schema.nodes.image.create({
                  src: placeholderSrc,
                  alt: file.name,
                  title: 'Uploading...',
                });

                const transaction = view.state.tr.insert(coordinates.pos, node);
                view.dispatch(transaction);

                try {
                  const url = await onUpload(file);

                  // Replace placeholder with actual URL
                  editor.commands.command(({ tr, state }) => {
                    let found = false;
                    state.doc.descendants((node, pos) => {
                      if (node.type.name === 'image' && node.attrs.src === placeholderSrc) {
                        tr.setNodeMarkup(pos, undefined, {
                          ...node.attrs,
                          src: url,
                          title: file.name,
                        });
                        found = true;
                        return false;
                      }
                      return true;
                    });
                    return found;
                  });

                  URL.revokeObjectURL(placeholderSrc);
                } catch (error) {
                  console.error('Image upload failed:', error);
                  // Remove placeholder on error
                  editor.commands.command(({ tr, state }) => {
                    state.doc.descendants((node, pos) => {
                      if (node.type.name === 'image' && node.attrs.src === placeholderSrc) {
                        tr.delete(pos, pos + node.nodeSize);
                        return false;
                      }
                      return true;
                    });
                    return true;
                  });
                  URL.revokeObjectURL(placeholderSrc);
                }
              }
            });

            return true;
          },

          handlePaste(view, event, _slice) {
            const files = Array.from(event.clipboardData?.files || []);
            const imageFiles = files.filter(file => file.type.startsWith('image/'));

            if (imageFiles.length === 0) return false;

            event.preventDefault();

            const { schema } = view.state;
            const { selection } = view.state;

            imageFiles.forEach(async (file) => {
              // Show loading placeholder
              const placeholderSrc = URL.createObjectURL(file);

              const node = schema.nodes.image.create({
                src: placeholderSrc,
                alt: file.name,
                title: 'Uploading...',
              });

              const transaction = view.state.tr.replaceSelectionWith(node);
              view.dispatch(transaction);

              try {
                const url = await onUpload(file);

                // Replace placeholder with actual URL
                editor.commands.command(({ tr, state }) => {
                  let found = false;
                  state.doc.descendants((node, pos) => {
                    if (node.type.name === 'image' && node.attrs.src === placeholderSrc) {
                      tr.setNodeMarkup(pos, undefined, {
                        ...node.attrs,
                        src: url,
                        title: file.name,
                      });
                      found = true;
                      return false;
                    }
                    return true;
                  });
                  return found;
                });

                URL.revokeObjectURL(placeholderSrc);
              } catch (error) {
                console.error('Image upload failed:', error);
                // Remove placeholder on error
                editor.commands.command(({ tr, state }) => {
                  state.doc.descendants((node, pos) => {
                    if (node.type.name === 'image' && node.attrs.src === placeholderSrc) {
                      tr.delete(pos, pos + node.nodeSize);
                      return false;
                    }
                    return true;
                  });
                  return true;
                });
                URL.revokeObjectURL(placeholderSrc);
              }
            });

            return true;
          },
        },
      }),
    ];
  },
});
