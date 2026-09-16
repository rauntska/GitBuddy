Screenshots referenced from `CHANGELOG.md` entries live here.

Vite serves everything in `public/` from the site root, so an image saved as
`gitbuddy-vue/public/changelog/<name>.png` is referenced from `CHANGELOG.md` as:

```markdown
![alt text](/changelog/<name>.png)
```

This works in both `npm run dev` and the built production bundle, with no upload
endpoint or backend change involved.
