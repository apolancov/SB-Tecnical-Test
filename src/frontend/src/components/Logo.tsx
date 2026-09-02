import Image from 'next/image';
import logoSource from '../../public/logo.png';

export interface LogoProps {
  readonly className?: string;
  readonly size?: 'small' | 'medium' | 'large';
  readonly alt?: string;
}

const SIZE_TO_HEIGHT: Readonly<Record<NonNullable<LogoProps['size']>, number>> = {
  small: 24,
  medium: 34,
  large: 56,
};

export function Logo({
  className,
  size = 'medium',
  alt = 'SB Client',
}: LogoProps) {
  const height = SIZE_TO_HEIGHT[size];
  return (
    <Image
      src={logoSource}
      alt={alt}
      height={height}
      width={height * 4}
      className={className}
      style={{ height, width: 'auto' }}
      data-testid="app-logo"
      priority
    />
  );
}
