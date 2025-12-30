import logging
import os
from typing import Optional
from src.domain.interfaces import ILogger


class StandardLogger(ILogger):
    """
    Python standard library implementation of our Logger. 🛡️⚖️🏛️
    """

    def __init__(
        self, name: str, log_file: Optional[str] = None, level: int = logging.INFO
    ):
        self.logger = logging.getLogger(name)
        self.logger.setLevel(level)

        if not self.logger.handlers:
            formatter = logging.Formatter(
                "%(asctime)s - %(name)s - %(levelname)s - %(message)s",
                datefmt="%Y-%m-%d %H:%M:%S",
            )

            # Console Handler
            console_handler = logging.StreamHandler()
            console_handler.setLevel(logging.INFO) # Only INFO and above to console
            console_handler.setFormatter(formatter)
            self.logger.addHandler(console_handler)

            # File Handler
            if log_file:
                os.makedirs(os.path.dirname(log_file), exist_ok=True)
                file_handler = logging.FileHandler(log_file, mode='w') # Changed mode to 'w' for overwrite
                file_handler.setLevel(level) # Keep original level for file
                file_handler.setFormatter(formatter)
                self.logger.addHandler(file_handler)

    def info(self, message: str):
        self.logger.info(message)

    def debug(self, message: str):
        self.logger.debug(message)

    def warning(self, message: str):
        self.logger.warning(message)

    def error(self, message: str):
        self.logger.error(message)


class NullLogger(ILogger):
    """The Null logger implementation. 🤫💖"""

    def info(self, message: str):
        pass

    def debug(self, message: str):
        pass

    def warning(self, message: str):
        pass

    def error(self, message: str):
        pass
