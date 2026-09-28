import { useState, type ComponentProps } from 'react';
import { imageSizes, imageSources, type ImageSize } from '@/lib/images';

interface ResponsiveImageProps extends Omit<ComponentProps<'img'>, 'src' | 'srcSet' | 'sizes'> {
  src: string;
  /** How wide the picture is shown; the browser loads the smallest copy that fills it. */
  size: ImageSize;
  /** Show a tiny blurred copy until the picture arrives; for the large ones. */
  placeholder?: boolean;
}

/**
 * A picture of the shop in the size it is shown at (ADR 016): the smaller copies in srcset, and
 * the tiny one behind it while it loads. A picture from elsewhere has no copies; should they be
 * missing all the same, the picture falls back to its own address.
 */
const ResponsiveImage = ({ src, size, placeholder = false, style, onError, ...rest }: ResponsiveImageProps) => {
  // The address whose copies failed to load; the picture shows the original then
  const [failed, setFailed] = useState<string | null>(null);
  const sources = failed === src ? { src } : imageSources(src);

  return (
    <img
      {...rest}
      src={sources.src}
      srcSet={sources.srcSet}
      sizes={sources.srcSet ? imageSizes[size] : undefined}
      style={
        placeholder && sources.placeholder
          ? { backgroundImage: `url("${sources.placeholder}")`, backgroundSize: 'cover', backgroundPosition: 'center', ...style }
          : style
      }
      onError={(event) => {
        if (sources.srcSet) setFailed(src);
        onError?.(event);
      }}
    />
  );
};

export default ResponsiveImage;
