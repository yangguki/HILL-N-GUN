import numpy as np
from PIL import Image
from scipy import ndimage
def key(img):
    a=np.asarray(img.convert('RGB')).astype(np.float32)
    h,w,_=a.shape
    border=np.concatenate([a[:4].reshape(-1,3),a[-4:].reshape(-1,3),a[:,:4].reshape(-1,3),a[:,-4:].reshape(-1,3)])
    bg=np.median(border,axis=0)
    d=np.sqrt(((a-bg)**2).sum(-1))
    al=np.clip((d-45)/(110-45),0,1)
    solid=al>0.5
    lab,n=ndimage.label(solid)
    if n:
        sizes=ndimage.sum(solid,lab,range(1,n+1))
        keep=np.isin(lab,1+np.where(sizes>=60)[0])
        keep=ndimage.binary_dilation(keep,iterations=2)
        al=al*keep
    # despill on edge zone
    edge=(al<1)|ndimage.binary_dilation(al<0.5,iterations=2)
    r,g,b=a[...,0],a[...,1],a[...,2]
    if bg[1]>bg[0] and bg[1]>bg[2]:  # green
        lim=np.maximum(r,b); g2=np.where(edge&(g>lim),lim,g); a[...,1]=g2
    else: # magenta
        lim=g+20; a[...,0]=np.where(edge&(r>lim)&(b>lim),np.minimum(r,lim+30),r); a[...,2]=np.where(edge&(r>lim)&(b>lim),np.minimum(b,lim+30),b)
    out=np.dstack([a,al*255]).clip(0,255).astype(np.uint8)
    return Image.fromarray(out,'RGBA')
def place(cut,canvas,scale,bottom_frac,maxfrac=0.97):
    bb=cut.getbbox(); c=cut.crop(bb)
    s=scale
    w,h=c.size
    fit=min(1,canvas*maxfrac/(w*s),canvas*maxfrac/(h*s)); s*=fit
    c=c.resize((max(1,round(w*s)),max(1,round(h*s))),Image.LANCZOS)
    out=Image.new('RGBA',(canvas,canvas),(0,0,0,0))
    x=(canvas-c.size[0])//2; y=round(canvas*bottom_frac)-c.size[1]; y=max(0,y)
    out.alpha_composite(c,(x,y)); return out
