import logging
import sys
from ..domain.interfaces import ILogger

class LocalLogger(ILogger):
    def __init__(self, log_file: str = "pipeline.log", name: str = "AudioPipeline", setup_handlers: bool = True):
        self.logger = logging.getLogger(name)
        self.logger.setLevel(logging.DEBUG)

        # We only setup handlers if we're at the root of our hierarchy! 🏛️💎
        if setup_handlers and not self.logger.handlers:
            # 1. Console Handler
            console_handler = logging.StreamHandler(sys.stdout)
            console_handler.setLevel(logging.INFO)
            console_formatter = logging.Formatter('%(message)s')
            console_handler.setFormatter(console_formatter)

            # 2. File Handler
            file_handler = logging.FileHandler(log_file)
            file_handler.setLevel(logging.DEBUG)
            file_formatter = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')
            file_handler.setFormatter(file_formatter)

            self.logger.addHandler(console_handler)
            self.logger.addHandler(file_handler)

    def get_child(self, name: str) -> 'LocalLogger':
        """Creates a child logger that propagates to the root! 👶💎"""
        child_name = f"{self.logger.name}.{name}"
        # We pass setup_handlers=False so it doesn't double-talk! 🤫
        return LocalLogger(name=child_name, setup_handlers=False)

    def info(self, message: str):
        self.logger.info(message)

    def debug(self, message: str):
        self.logger.debug(message)

    def error(self, message: str):
        self.logger.error(message)
