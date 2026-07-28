import React, { useRef, useEffect } from 'react';
import LoadingBar, { LoadingBarRef } from 'react-top-loading-bar';

const TopLoadingBar: React.FC = () => {
  const ref = useRef<LoadingBarRef>(null);

  useEffect(() => {
    const handleStart = () => {
      ref.current?.continuousStart();
    };

    const handleComplete = () => {
      ref.current?.complete();
    };

    window.addEventListener('loading-bar:start', handleStart);
    window.addEventListener('loading-bar:complete', handleComplete);

    return () => {
      window.removeEventListener('loading-bar:start', handleStart);
      window.removeEventListener('loading-bar:complete', handleComplete);
    };
  }, []);

  return <LoadingBar ref={ref} color="#3b82f6" height={3} />;
};

export default TopLoadingBar;
